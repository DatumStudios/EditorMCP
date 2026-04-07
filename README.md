# EditorMCP

EditorMCP is a Unity Editor MCP (Model Context Protocol) server and tooling package that lets MCP-capable clients interact with your Unity project through a controlled editor surface.

This repository distributes the **free Base tier** package only.

## What Base Includes

- Unity Editor MCP server integration
- Editor UI under `Window -> EditorMCP`
- Base-tier tool surface for foundational project interaction
- Built-in health/self-test flow for setup validation

## What Is Not In This Repo

- Core, Pro, and Studio tiers are not distributed from this repository
- This repository is Base-only distribution
- Runtime/build target support is not included (Editor-only package)

## Install (Unity Package Manager)

Add this dependency to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.datumstudios.editormcp.base": "https://github.com/DatumStudios/EditorMCP.git?path=/package/com.datumstudios.editormcp.base"
  }
}
```

You can also install from Unity Package Manager with **Add package from Git URL** using:

`https://github.com/DatumStudios/EditorMCP.git?path=/package/com.datumstudios.editormcp.base`

## Quick Start

1. Install the package via the UPM Git URL above.
2. In Unity, open `Window -> EditorMCP -> Status`.
3. Start the EditorMCP server.
4. From your MCP client, run `mcp.selfTest`.

## Platform Support

- Windows: supported
- macOS: may work, not officially supported
- Editor-only package: no Runtime/build support

## Tier Overview

- **Base**: free tier (distributed in this repo)
- **Core / Pro / Studio**: higher tiers available separately

## License

EditorMCP is proprietary software.

Third-party attributions and notices are provided in:

- `package/com.datumstudios.editormcp.base/ThirdPartyLicenses.md`
