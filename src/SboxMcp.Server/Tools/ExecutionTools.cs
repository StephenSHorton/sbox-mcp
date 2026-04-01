using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class ExecutionTools
{
    [McpServerTool(Name = "execute_csharp")]
    [Description("Execute a C# expression or statement in the s&box editor context. Returns the result or output.")]
    public static async Task<string> ExecuteCSharp(
        EditorBridgeServer bridge,
        [Description("C# code to execute")] string code,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("execute.csharp", new { code }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no output)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "console_run")]
    [Description("Run a console command in the s&box console")]
    public static async Task<string> ConsoleRun(
        EditorBridgeServer bridge,
        [Description("Console command to run")] string command,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("console.run", new { command }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no output)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "get_bridge_status")]
    [Description("Check if the s&box editor bridge is connected")]
    public static Task<string> GetBridgeStatus(EditorBridgeServer bridge, CancellationToken ct)
    {
        var status = bridge.IsConnected ? "connected" : "disconnected";
        var result = $"Bridge status: {status}\nBridge URL: ws://localhost:{bridge.Port}/";
        return Task.FromResult(result);
    }
}
