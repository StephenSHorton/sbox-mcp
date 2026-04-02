using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SboxMcp.Server.Bridge;

public sealed class EditorBridgeServer : BackgroundService
{
    private readonly ILogger<EditorBridgeServer> _logger;
    private readonly int _port;
    private WebSocket? _client;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BridgeResponse>> _pending = new();

    public bool IsConnected => _client is { State: WebSocketState.Open };
    public int Port => _port;

    public EditorBridgeServer(ILogger<EditorBridgeServer> logger)
    {
        _logger = logger;
        _port = int.TryParse(Environment.GetEnvironmentVariable("SBOX_MCP_PORT"), out var p) ? p : 29015;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/");

        try
        {
            listener.Start();
        }
        catch (HttpListenerException)
        {
            _logger.LogWarning("Port {Port} already in use — killing stale SboxMcp.Server process", _port);
            KillExistingServer();
            listener.Start();
        }

        _logger.LogInformation("WebSocket server listening on http://localhost:{Port}/", _port);

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync().WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!ctx.Request.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                continue;
            }

            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            _client = wsCtx.WebSocket;
            _logger.LogInformation("s&box bridge connected from {RemoteEndPoint}", ctx.Request.RemoteEndPoint);

            await ReceiveLoopAsync(_client, stoppingToken);

            _logger.LogInformation("s&box bridge disconnected");
            _client = null;

            // Fail all pending requests
            foreach (var (key, tcs) in _pending)
            {
                if (_pending.TryRemove(key, out _))
                    tcs.TrySetException(new InvalidOperationException("Bridge disconnected."));
            }
        }

        listener.Stop();
    }

    private async Task ReceiveLoopAsync(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                try
                {
                    result = await ws.ReceiveAsync(buffer, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (WebSocketException)
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            BridgeResponse? response;
            try
            {
                response = await JsonSerializer.DeserializeAsync<BridgeResponse>(ms, cancellationToken: ct);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize bridge response");
                continue;
            }

            if (response is null) continue;

            if (_pending.TryRemove(response.Id, out var tcs))
                tcs.TrySetResult(response);
            else
                _logger.LogDebug("Received response for unknown id {Id}", response.Id);
        }
    }

    private void KillExistingServer()
    {
        var current = Environment.ProcessId;
        foreach (var proc in Process.GetProcessesByName("SboxMcp.Server"))
        {
            if (proc.Id == current) continue;
            try
            {
                _logger.LogInformation("Killing stale SboxMcp.Server (PID {Pid})", proc.Id);
                proc.Kill();
                proc.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill PID {Pid}", proc.Id);
            }
        }
    }

    public async Task<BridgeResponse> SendCommandAsync(string command, object? @params, CancellationToken ct)
    {
        if (_client is not { State: WebSocketState.Open })
            throw new InvalidOperationException(
                "No s&box editor bridge is connected. Please start the SboxMcp bridge addon inside the s&box editor.");

        var id = Guid.NewGuid().ToString();
        var request = new BridgeRequest { Id = id, Command = command, Params = @params };
        var json = JsonSerializer.SerializeToUtf8Bytes(request);

        var tcs = new TaskCompletionSource<BridgeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        try
        {
            await _client.SendAsync(json, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        catch
        {
            _pending.TryRemove(id, out _);
            throw;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _pending.TryRemove(id, out _);
            throw new TimeoutException($"Timed out waiting for bridge response to command '{command}'.");
        }
    }
}
