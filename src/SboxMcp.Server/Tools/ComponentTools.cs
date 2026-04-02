using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class ComponentTools
{
    [McpServerTool(Name = "component_list")]
    [Description("List all components on a GameObject")]
    public static async Task<string> ListComponents(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("component.list", new { id = objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no components)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "component_get")]
    [Description("Get a component's properties and values")]
    public static async Task<string> GetComponent(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The component type name")] string componentType,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("component.get", new { id = objectId, type = componentType }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "component_set")]
    [Description("Set a property value on a component")]
    public static async Task<string> SetComponent(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The component type name")] string componentType,
        [Description("The property name to set")] string property,
        [Description("The value to assign")] string value,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("component.set", new { id = objectId, type = componentType, property, value }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Property updated."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "component_add")]
    [Description("Add a component to a GameObject")]
    public static async Task<string> AddComponent(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The component type name to add")] string componentType,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("component.add", new { id = objectId, type = componentType }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Component added."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "component_remove")]
    [Description("Remove a component from a GameObject")]
    public static async Task<string> RemoveComponent(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject")] string objectId,
        [Description("The component type name to remove")] string componentType,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("component.remove", new { id = objectId, type = componentType }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Component removed."
            : $"Error: {response.Error}";
    }
}
