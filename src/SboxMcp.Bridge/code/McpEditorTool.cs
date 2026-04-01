namespace SboxMcp.Bridge;

/// <summary>
/// s&box Editor Dock entry point for the MCP Bridge.
/// Appears in the editor as a dockable panel and manages the WebSocket client lifecycle.
/// </summary>
[Dock( "Editor", "MCP Bridge", "smart_toy" )]
public class McpBridgeDock : Widget
{
	private McpBridgeClient _client;

	private int _port = 29015;
	private readonly List<string> _recentCommands = new();
	private const int MaxLogEntries = 50;

	private Label _statusLabel;
	private Button _connectButton;
	private Layout _logLayout;

	public McpBridgeDock( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		// --- Status row ---
		var statusRow = Layout.AddRow();
		statusRow.Spacing = 4;
		_statusLabel = new Label( "Status: Disconnected", this );
		statusRow.Add( _statusLabel );

		// --- Port row ---
		var portRow = Layout.AddRow();
		portRow.Spacing = 4;
		new Label( "Port:", this );
		var portField = new LineEdit( this );
		portField.Text = _port.ToString();
		portField.TextEdited += v =>
		{
			if ( int.TryParse( v, out var p ) )
				_port = p;
		};
		portRow.Add( new Label( "Port:", this ) );
		portRow.Add( portField );

		// --- Connect button ---
		_connectButton = new Button( "Connect", this );
		_connectButton.Clicked = () =>
		{
			if ( _client != null && _client.IsConnected )
				StopClient();
			else
				StartClient();
		};
		Layout.Add( _connectButton );

		// --- Log area ---
		Layout.Add( new Label( "Recent Commands:", this ) );
		var scroll = new ScrollArea( this );
		scroll.Canvas = new Widget( this );
		scroll.Canvas.Layout = Layout.Column();
		_logLayout = scroll.Canvas.Layout;
		_logLayout.Spacing = 2;
		Layout.Add( scroll, 1 );

		// Auto-start connection when the editor loads.
		StartClient();
	}

	public override void OnDestroyed()
	{
		StopClient();
		base.OnDestroyed();
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		var connected = _client != null && _client.IsConnected;
		_statusLabel.Text = connected ? "Status: Connected" : "Status: Disconnected";
		_connectButton.Text = connected ? "Disconnect" : "Connect";
	}

	private void StartClient()
	{
		StopClient();
		var url = $"ws://localhost:{_port}";
		_client = new McpBridgeClient( url );
		_client.Connect();
		Log.Info( $"[MCP Bridge] Client started, connecting to {url}" );
		AddLog( $"Connecting to {url}..." );
	}

	private void StopClient()
	{
		if ( _client is null )
			return;
		_client.Disconnect();
		_client.Dispose();
		_client = null;
		AddLog( "Disconnected." );
	}

	private void AddLog( string message )
	{
		var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
		_recentCommands.Insert( 0, entry );
		if ( _recentCommands.Count > MaxLogEntries )
			_recentCommands.RemoveAt( _recentCommands.Count - 1 );

		// Rebuild log UI
		_logLayout.Clear( true );
		foreach ( var e in _recentCommands )
			_logLayout.Add( new Label( e, this ) );
	}
}
