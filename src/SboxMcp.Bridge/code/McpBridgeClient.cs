using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace SboxMcp.Bridge;

/// <summary>
/// Incoming request from the MCP server.
/// </summary>
public class BridgeRequest
{
	[JsonPropertyName( "id" )]
	public string Id { get; set; } = "";

	[JsonPropertyName( "command" )]
	public string Command { get; set; } = "";

	[JsonPropertyName( "params" )]
	public JsonElement? Params { get; set; }
}

/// <summary>
/// Outgoing response sent back to the MCP server.
/// </summary>
public class BridgeResponse
{
	[JsonPropertyName( "id" )]
	public string Id { get; set; } = "";

	[JsonPropertyName( "success" )]
	public bool Success { get; set; }

	[JsonPropertyName( "data" )]
	[JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
	public object Data { get; set; }

	[JsonPropertyName( "error" )]
	[JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
	public string Error { get; set; }

	public static BridgeResponse Ok( string id, object data = null ) =>
		new() { Id = id, Success = true, Data = data };

	public static BridgeResponse Fail( string id, string error ) =>
		new() { Id = id, Success = false, Error = error };
}

/// <summary>
/// WebSocket client that connects to the MCP server and dispatches commands.
/// </summary>
public class McpBridgeClient : IDisposable
{
	public const string DefaultUrl = "ws://localhost:29015";
	private const int ReconnectDelayMs = 3000;
	private const int ReceiveBufferSize = 8192;

	private readonly string _url;
	private ClientWebSocket _ws;
	private CancellationTokenSource _cts;
	private readonly SemaphoreSlim _sendLock = new( 1, 1 );
	private bool _disposed;

	public bool IsConnected => _ws?.State == WebSocketState.Open;

	public McpBridgeClient( string url = DefaultUrl )
	{
		_url = url;
	}

	/// <summary>
	/// Starts the connection loop. Runs until Disconnect() is called.
	/// </summary>
	public void Connect()
	{
		if ( _cts is not null )
			return;

		_cts = new CancellationTokenSource();
		_ = RunConnectionLoop( _cts.Token );
	}

	/// <summary>
	/// Stops the connection and cancels any pending operations.
	/// </summary>
	public void Disconnect()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;

		try
		{
			_ws?.CloseAsync( WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None )
			    .GetAwaiter()
			    .GetResult();
		}
		catch { /* best-effort close */ }

		_ws?.Dispose();
		_ws = null;

		Log.Info( "[MCP Bridge] Disconnected." );
	}

	private async Task RunConnectionLoop( CancellationToken ct )
	{
		while ( !ct.IsCancellationRequested )
		{
			try
			{
				await AttemptConnect( ct );
				await ReceiveLoop( ct );
			}
			catch ( OperationCanceledException ) when ( ct.IsCancellationRequested )
			{
				break;
			}
			catch ( Exception ex )
			{
				Log.Warning( $"[MCP Bridge] Connection error: {ex.Message}" );
			}

			if ( ct.IsCancellationRequested )
				break;

			Log.Info( $"[MCP Bridge] Reconnecting in {ReconnectDelayMs}ms..." );
			await Task.Delay( ReconnectDelayMs, ct );
		}
	}

	private async Task AttemptConnect( CancellationToken ct )
	{
		_ws?.Dispose();
		_ws = new ClientWebSocket();

		Log.Info( $"[MCP Bridge] Connecting to {_url}..." );
		await _ws.ConnectAsync( new Uri( _url ), ct );
		Log.Info( "[MCP Bridge] Connected to MCP server." );
	}

	private async Task ReceiveLoop( CancellationToken ct )
	{
		var buffer = new byte[ReceiveBufferSize];
		var messageBuffer = new System.IO.MemoryStream();

		while ( _ws.State == WebSocketState.Open && !ct.IsCancellationRequested )
		{
			messageBuffer.SetLength( 0 );
			WebSocketReceiveResult result;

			do
			{
				result = await _ws.ReceiveAsync( new ArraySegment<byte>( buffer ), ct );

				if ( result.MessageType == WebSocketMessageType.Close )
				{
					Log.Info( "[MCP Bridge] Server closed connection." );
					await _ws.CloseAsync( WebSocketCloseStatus.NormalClosure, "Closing", ct );
					return;
				}

				messageBuffer.Write( buffer, 0, result.Count );
			}
			while ( !result.EndOfMessage );

			var json = Encoding.UTF8.GetString( messageBuffer.ToArray() );
			_ = ProcessMessage( json );
		}
	}

	private async Task ProcessMessage( string json )
	{
		BridgeRequest request = null;

		try
		{
			request = JsonSerializer.Deserialize<BridgeRequest>( json );
			if ( request is null )
			{
				Log.Warning( "[MCP Bridge] Received null or invalid message." );
				return;
			}

			Log.Info( $"[MCP Bridge] Command received: {request.Command} (id={request.Id})" );
			var response = await CommandRouter.Route( request );
			await SendResponse( response );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[MCP Bridge] Error processing message: {ex.Message}" );

			if ( request is not null )
			{
				await SendResponse( BridgeResponse.Fail( request.Id, ex.Message ) );
			}
		}
	}

	/// <summary>
	/// Sends a response back to the MCP server. Thread-safe.
	/// </summary>
	public async Task SendResponse( BridgeResponse response )
	{
		if ( !IsConnected )
			return;

		var json = JsonSerializer.Serialize( response );
		var bytes = Encoding.UTF8.GetBytes( json );

		await _sendLock.WaitAsync();
		try
		{
			await _ws.SendAsync(
				new ArraySegment<byte>( bytes ),
				WebSocketMessageType.Text,
				endOfMessage: true,
				CancellationToken.None );
		}
		finally
		{
			_sendLock.Release();
		}
	}

	public void Dispose()
	{
		if ( _disposed )
			return;
		_disposed = true;
		Disconnect();
		_sendLock.Dispose();
	}
}
