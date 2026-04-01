using System.ComponentModel;
using ModelContextProtocol.Server;
using SboxMcp.Server.Bridge;

namespace SboxMcp.Server.Tools;

[McpServerToolType]
public static class EditorTools
{
    [McpServerTool(Name = "editor_get_selection")]
    [Description("Get the currently selected GameObjects in the editor")]
    public static async Task<string> GetSelection(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.get_selection", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "editor_select_object")]
    [Description("Select a GameObject in the editor by ID")]
    public static async Task<string> SelectObject(
        EditorBridgeServer bridge,
        [Description("The ID of the GameObject to select")] string objectId,
        CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.select", new { objectId }, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Selected."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "editor_undo")]
    [Description("Undo the last editor action")]
    public static async Task<string> Undo(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.undo", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Undone."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "editor_redo")]
    [Description("Redo the last undone editor action")]
    public static async Task<string> Redo(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.redo", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Redone."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "editor_save_scene")]
    [Description("Save the current scene")]
    public static async Task<string> SaveScene(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.save_scene", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "Scene saved."
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "editor_take_screenshot")]
    [Description("Take a screenshot of the editor viewport. Returns the file path of the saved screenshot.")]
    public static async Task<string> TakeScreenshot(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("editor.screenshot", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(no path returned)"
            : $"Error: {response.Error}";
    }

    [McpServerTool(Name = "scene_get_hierarchy")]
    [Description("Get the full scene hierarchy as indented text for easy reading")]
    public static async Task<string> GetHierarchy(EditorBridgeServer bridge, CancellationToken ct)
    {
        var response = await bridge.SendCommandAsync("scene.hierarchy", null, ct);
        return response.Success
            ? response.Data?.ToString() ?? "(empty scene)"
            : $"Error: {response.Error}";
    }
}
