# sbox-mcp

MCP server for the s&box game engine editor.

## Architecture

Two-component system:

- **SboxMcp.Server** (`src/SboxMcp.Server/`) — .NET 9 MCP server. Speaks stdio to AI clients, hosts a WebSocket server on port 29015 for the editor bridge. Build with `dotnet build`.
- **SboxMcp.Bridge** (`src/SboxMcp.Bridge/`) — s&box editor addon (C# source compiled by s&box's Roslyn pipeline). NOT a .NET project — do not try to `dotnet build` it. Install by copying files into `addons/tools/Code/McpBridge/` inside the s&box install directory.

## Communication Protocol

WebSocket JSON messages between server and bridge:

```
Request:  { "id": "uuid", "command": "scene.list", "params": {} }
Response: { "id": "uuid", "success": true, "data": { ... } }
Error:    { "id": "uuid", "success": false, "error": "message" }
```

## s&box API Notes

- The bridge compiles as part of `local.toolbase` — files go in `addons/tools/Code/McpBridge/`
- s&box editor widgets extend `Widget` with `[Dock("Editor", "Name", "icon")]`, NOT `EditorWindow`
- Use `EditorTypeLibrary` (not `TypeLibrary`) for editor-context type lookups
- Use `SceneEditorSession.Active` for selection, undo, save operations
- Use `Rotation.From(pitch, yaw, roll)` not `FromEulerAngles`
- Disambiguate `Sandbox.FileSystem` and `Sandbox.ConsoleSystem` explicitly (both Editor and Sandbox namespaces export them)
- Global imports (`Editor`, `Sandbox`, `System`, etc.) are provided by `Imports.cs` — do not add using statements for these
- Do not use nullable reference annotations (`string?`) — s&box compiles without `#nullable enable`
- Widget lifecycle: constructor for setup, `[EditorEvent.Frame]` for updates, `OnDestroyed()` for cleanup
- Use `.IsValid()` to check if a widget is still alive, not null checks
- Toast notifications via `ToastWidget` / `ToastManager`

## Build & Test

```bash
# Build the MCP server
dotnet build src/SboxMcp.Server -c Release

# Run the MCP server (for testing — normally launched by Claude Code)
dotnet run --project src/SboxMcp.Server

# Sync bridge files to s&box (adjust path to your s&box install)
cp -r src/SboxMcp.Bridge/code/* "/c/Program Files (x86)/Steam/steamapps/common/sbox/addons/tools/Code/McpBridge/"
```

## Documentation

Keep `README.md` up-to-date with any user-facing changes. The README is the first thing someone sees when they visit the repo. If a change adds/removes tools, modifies setup steps, changes configuration, or alters the architecture, update the README in the same commit. The tools table and setup instructions are the most common sections that need updating.

## Adding New Tools

1. Add the MCP tool method in `src/SboxMcp.Server/Tools/` (use `[McpServerTool]` attribute)
2. Add the bridge handler in `src/SboxMcp.Bridge/code/Handlers/`
3. Register the command in `CommandRouter.cs`
4. Copy updated bridge files to the s&box install directory
5. s&box hot-reloads C# changes — restart only needed if compilation fails
