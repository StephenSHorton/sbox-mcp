using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class FileTools
{
    [McpServerTool(Name = "file_read")]
    [Description("Read a file from the s&box project")]
    public static async Task<string> ReadFile(
        EditorBridgeServer bridge,
        [Description("Path to the file relative to the project root")] string path,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("file.read", new { path }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty file)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "file_write")]
    [Description("Write content to a file in the s&box project")]
    public static async Task<string> WriteFile(
        EditorBridgeServer bridge,
        [Description("Path to the file relative to the project root")] string path,
        [Description("Content to write to the file")] string content,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("file.write", new { path, content }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "File written."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "file_list")]
    [Description("List files in the s&box project directory")]
    public static async Task<string> ListFiles(
        EditorBridgeServer bridge,
        [Description("Directory to list, defaults to project root")] string? directory,
        [Description("Optional glob pattern to filter results, e.g. \"*.cs\"")] string? pattern,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("file.list", new { directory, pattern }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no files)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "project_info")]
    [Description("Get s&box project metadata and status")]
    public static async Task<string> ProjectInfo(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("project.info", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }
}
