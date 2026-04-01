# sbox-mcp — MCP Server for s&box

[![Build](https://github.com/StephenSHorton/sbox-mcp/actions/workflows/build.yml/badge.svg)](https://github.com/StephenSHorton/sbox-mcp/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![s&box](https://img.shields.io/badge/s%26box-editor-blue)](https://sbox.game)

AI-powered editor automation for the s&box game engine via the Model Context Protocol.

---

## Architecture

```
┌─────────────┐     stdio      ┌──────────────┐   WebSocket    ┌────────────────┐
│  AI Client   │◄──────────────►│  MCP Server   │◄─────────────►│  s&box Editor   │
│ (Claude,etc) │                │  (.NET 9)     │  :29015       │  Bridge Addon   │
└─────────────┘                └──────────────┘                └────────────────┘
```

The MCP Server exposes tools over **stdio** (consumed by AI clients like Claude Desktop). It forwards commands over a **WebSocket** connection to the Bridge Addon running inside the s&box editor, which executes them against the live scene and returns results.

---

## Features

- Scene manipulation — create, read, update, and delete GameObjects
- Component editing — inspect and modify component properties at runtime
- File operations — read and write project files
- Code execution — run arbitrary C# expressions in the editor context
- Console commands — invoke editor console commands programmatically

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [s&box editor](https://sbox.game) (for the Bridge Addon)

---

## Setup

### 1. MCP Server

Clone the repository and build the server:

```bash
git clone https://github.com/StephenSHorton/sbox-mcp.git
cd sbox-mcp
dotnet build sbox-mcp.sln --configuration Release
```

Add the server to your AI client's MCP configuration (e.g. Claude Desktop's `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "sbox": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/src/SboxMcp.Server"]
    }
  }
}
```

Replace `path/to/src/SboxMcp.Server` with the absolute path to the project directory on your machine.

### 2. s&box Bridge Addon

Copy the `src/SboxMcp.Bridge` directory into your s&box addons directory:

```
%APPDATA%\sbox\addons\SboxMcp.Bridge\
```

Alternatively, install it from the s&box asset library once it is published. The addon starts automatically when the s&box editor opens and listens for WebSocket connections on port `29015`.

---

## Available Tools

| Tool | Description |
|------|-------------|
| `scene_list_objects` | List all GameObjects in the scene |
| `scene_get_object` | Get detailed GameObject info |
| `scene_create_object` | Create a new GameObject |
| `scene_delete_object` | Delete a GameObject |
| `scene_find_objects` | Search GameObjects by name |
| `scene_set_transform` | Set position/rotation/scale |
| `component_list` | List components on a GameObject |
| `component_get` | Get component properties |
| `component_set` | Set a component property |
| `component_add` | Add a component |
| `component_remove` | Remove a component |
| `file_read` | Read a project file |
| `file_write` | Write a project file |
| `file_list` | List project files |
| `project_info` | Get project metadata |
| `execute_csharp` | Execute C# in the editor |
| `console_run` | Run a console command |
| `get_bridge_status` | Check bridge connection |

---

## Configuration

The server can be configured via environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `SBOX_MCP_PORT` | `29015` | WebSocket port the server connects to on the Bridge Addon |

---

## Development

### Build

```bash
dotnet build sbox-mcp.sln
```

### Run

```bash
dotnet run --project src/SboxMcp.Server
```

The server communicates over stdio; pipe it through your MCP client or test it manually with a tool like `mcp-inspector`.

### Project Structure

```
sbox-mcp/
├── src/
│   ├── SboxMcp.Server/       # .NET 9 MCP server (stdio transport)
│   └── SboxMcp.Bridge/       # s&box Roslyn addon (WebSocket listener)
├── .github/
│   └── workflows/
│       └── build.yml         # CI pipeline
├── sbox-mcp.sln
└── README.md
```

---

## How It Works

1. An AI client (Claude, Cursor, etc.) launches the MCP Server as a subprocess and communicates with it over **stdio** using the Model Context Protocol.
2. When the AI calls a tool, the MCP Server serializes the request and sends it over a **WebSocket** to the Bridge Addon running inside the s&box editor.
3. The Bridge Addon executes the request against the live scene (reading/writing GameObjects, components, files, etc.) and returns a JSON response.
4. The MCP Server forwards the response back to the AI client over stdio.

This design keeps the .NET 9 server fully decoupled from s&box's proprietary Roslyn pipeline, while the Bridge Addon stays thin and focused on editor-side execution.

---

## Contributing

Pull requests are welcome. For significant changes, please open an issue first to discuss what you would like to change.

This project is licensed under the [MIT License](LICENSE).

---

## Acknowledgments

- [Facepunch Studios](https://facepunch.com) for the s&box game engine and editor
- [Anthropic](https://anthropic.com) for the [Model Context Protocol](https://modelcontextprotocol.io) specification
