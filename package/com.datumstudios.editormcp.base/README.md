# EditorMCP Base

EditorMCP Base is a Unity Editor tool that runs an MCP (Model Context Protocol) server inside Unity, allowing external AI tools to safely inspect and interact with your project.

## What Base Includes

- EditorMCP Base tier tool surface
- Editor UI under `Window -> EditorMCP`
- STDIO and WebSocket transport support
- Built-in self-test and diagnostics

## What Base Does Not Include

- Core / Pro / Studio tier functionality
- No samples are shipped
- `Runtime/` content (Editor-only package)
- PDB debug symbol files

## Install (Unity Package Manager)

Add via Git URL.

In Unity:

1. Open `Window -> Package Manager`
2. Click `+ -> Add package from Git URL`
3. Paste:

`https://github.com/DatumStudios/EditorMCP.git?path=/package/com.datumstudios.editormcp.base#main`

## Expected Result After Install

- No console errors or warnings
- Menu appears: `Window -> EditorMCP`
- EditorMCP window opens
- Server can be started

## Open the UI

In Unity, go to:

`Window -> EditorMCP -> Status`

Start the server (or enable auto-start).

## Run Self-Test

After the server is running, call `mcp.selfTest` from your MCP client.

Expected result:

- PASS
- 0 warnings
- 0 failures

## Basic Usage Example

Connect your MCP client to:

- STDIO, or
- `ws://127.0.0.1:27182/`

Then call:

1. `tools/list`
2. `project.info`

## Troubleshooting

**Menu missing**

- Check Unity Console for compile errors.

**Server not responding**

- Restart Unity.
- Reopen the EditorMCP window.

**Port conflict**

- Default port: `27182`
- Ensure the port is free.

**Install issues**

- Confirm dependency name: `com.datumstudios.editormcp.base`
- Use Unity `2022.3` or newer.

## Platform Support

- Windows (supported)
- macOS (may work, not supported)
- Runtime/build targets not supported (Editor-only)

## Summary

EditorMCP Base provides a safe, structured bridge between Unity and AI tools, giving you a stable foundation for MCP-based workflows inside the Unity Editor.