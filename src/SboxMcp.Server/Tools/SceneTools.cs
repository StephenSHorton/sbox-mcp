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
        var response = await bridge.SendCommandAsync("scene.get", new { id = objectId }, ct);
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
        var response = await bridge.SendCommandAsync("scene.delete", new { id = objectId }, ct);
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
        var response = await bridge.SendCommandAsync("scene.find", new { pattern = query }, ct);
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
        var response = await bridge.SendCommandAsync("scene.set_transform", new { id = objectId, position, rotation, scale }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Transform updated."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_clone_object")]
    [Description("Clone a GameObject in the scene")]
    public static async Task<string> CloneObject(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to clone")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.clone", new { id = objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_reparent_object")]
    [Description("Reparent a GameObject to a new parent")]
    public static async Task<string> ReparentObject(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to reparent")] string objectId,
        [Description("The ID of the new parent GameObject")] string parentId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.reparent", new { id = objectId, parentId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Reparented."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_find_by_component")]
    [Description("Find all GameObjects that have a specific component type")]
    public static async Task<string> FindByComponent(
        EditorBridgeServer bridge,
        [Description("The component type name to search for")] string componentType,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.find_by_component", new { type = componentType }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no results)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_find_by_tag")]
    [Description("Find all GameObjects that have a specific tag")]
    public static async Task<string> FindByTag(
        EditorBridgeServer bridge,
        [Description("The tag to search for")] string tag,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.find_by_tag", new { tag }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no results)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_load")]
    [Description("Load a scene from a file path")]
    public static async Task<string> LoadScene(
        EditorBridgeServer bridge,
        [Description("The file path of the scene to load")] string path,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.load", new { path }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Scene loaded."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "tag_add")]
    [Description("Add a tag to a GameObject")]
    public static async Task<string> TagAdd(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The tag to add")] string tag,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("tag.add", new { id = objectId, tag }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Tag added."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "tag_remove")]
    [Description("Remove a tag from a GameObject")]
    public static async Task<string> TagRemove(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The tag to remove")] string tag,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("tag.remove", new { id = objectId, tag }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Tag removed."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "tag_list")]
    [Description("List all tags on a GameObject")]
    public static async Task<string> TagList(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("tag.list", new { id = objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no tags)"
            : $"Error: {response.Error}";
    }
}
