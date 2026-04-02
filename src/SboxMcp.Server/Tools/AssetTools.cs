using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class AssetTools
{
    [McpServerTool(Name = "asset_search")]
    [Description("Search for assets in the s&box asset library")]
    public static async Task<string> SearchAssets(
        EditorBridgeServer bridge,
        [Description("Search query string")] string query,
        [Description("Optional asset type filter")] string? type,
        [Description("Optional maximum number of results")] string? amount,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("asset.search", new { query, type, amount }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no results)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "asset_fetch")]
    [Description("Fetch detailed information about an asset by its identifier")]
    public static async Task<string> FetchAsset(
        EditorBridgeServer bridge,
        [Description("The asset identifier")] string ident,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("asset.fetch", new { ident }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "asset_mount")]
    [Description("Mount an asset into the current project by its identifier")]
    public static async Task<string> MountAsset(
        EditorBridgeServer bridge,
        [Description("The asset identifier to mount")] string ident,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("asset.mount", new { ident }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Asset mounted."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "asset_browse_local")]
    [Description("Browse local project assets by directory and/or file extension")]
    public static async Task<string> BrowseLocalAssets(
        EditorBridgeServer bridge,
        [Description("Optional directory path to browse")] string? directory,
        [Description("Optional file extension filter (e.g. vmat, vmdl)")] string? extension,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("asset.browse_local", new { directory, extension }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no assets)"
            : $"Error: {response.Error}";
    }
}
