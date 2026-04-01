using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class SceneTools
{
    [McpServerTool(Name = "scene_list_objects")]
    [Description("List all GameObjects in the current s&box scene hierarchy")]
    public static async Task<string> ListObjects(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.list", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_get_object")]
    [Description("Get detailed info about a GameObject including components and properties")]
    public static async Task<string> GetObject(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to retrieve")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.get", new { objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_create_object")]
    [Description("Create a new GameObject in the scene")]
    public static async Task<string> CreateObject(
        EditorBridgeServer bridge,
        [Description("Name for the new GameObject")] string name,
        [Description("Optional parent GameObject ID")] string? parentId,
        [Description("Optional position as \"x,y,z\" string")] string? position,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.create", new { name, parentId, position }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_delete_object")]
    [Description("Delete a GameObject from the scene")]
    public static async Task<string> DeleteObject(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to delete")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.delete", new { objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Deleted."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_find_objects")]
    [Description("Search for GameObjects by name (supports * wildcards)")]
    public static async Task<string> FindObjects(
        EditorBridgeServer bridge,
        [Description("Search query, supports * wildcards")] string query,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.find", new { query }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no results)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_set_transform")]
    [Description("Set a GameObject's position, rotation, and/or scale")]
    public static async Task<string> SetTransform(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to transform")] string objectId,
        [Description("Optional position as \"x,y,z\" string")] string? position,
        [Description("Optional rotation as \"x,y,z\" string")] string? rotation,
        [Description("Optional scale as \"x,y,z\" string")] string? scale,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.set_transform", new { objectId, position, rotation, scale }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Transform updated."
            : $"Error: {response.Error}";
    }
}
