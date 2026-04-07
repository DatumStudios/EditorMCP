# EditorMCP

**EditorMCP** is a Unity Editor package that hosts an MCP (Model Context Protocol) server so MCP-capable clients can work with your project through a structured, editor-native integration.

If you use AI assistants or automation that speak MCP, EditorMCP connects them to Unity without shipping player/runtime code in this distribution.

---

## What this repository contains

- **One UPM package:** `package/com.datumstudios.editormcp.base` — the **free Base tier** only.
- **Public install:** add the package via Git URL (see below), pinned to release tag **`v1.0.0`**.

Core, Pro, and Studio tiers are **not** published from this repo.

---

## What Base includes

- MCP server integration inside the Unity Editor
- Editor UI under `Window -> EditorMCP`
- A Base-tier tool surface for everyday editor and project tasks
- Health and self-test flows to validate setup

---

## What is not in this repo

- Core / Pro / Studio packages or payloads
- Samples
- Runtime or player build support — **Editor-only**

---

## Install (Unity Package Manager)

**Requirements:** Unity **2022.3** or newer (LTS recommended).

Add to your project’s `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.datumstudios.editormcp.base": "https://github.com/DatumStudios/EditorMCP.git?path=/package/com.datumstudios.editormcp.base#v1.0.0"
  }
}
```

Or in **Package Manager → + → Add package from Git URL**, paste:

```
https://github.com/DatumStudios/EditorMCP.git?path=/package/com.datumstudios.editormcp.base#v1.0.0
```

---

## Quick start

1. After install, let Unity finish importing and compiling.
2. Open **`Window -> EditorMCP -> Status`**.
3. Start the EditorMCP server (or enable auto-start in settings if available).
4. From your MCP client, call **`mcp.selfTest`** to confirm the connection.

---

## Expected result after install

- No EditorMCP-related compilation errors in the Console.
- **`Window -> EditorMCP`** appears in the menu.
- The status window opens and the server can be started.
- **`mcp.selfTest`** completes successfully from your client once connected.

Package-specific notes and troubleshooting: see **`package/com.datumstudios.editormcp.base/README.md`**.

---

## Platform support

| Platform | Support |
|----------|---------|
| **Windows** | Supported |
| **macOS** | May work; not officially supported for v1 |
| **Player / runtime builds** | Not applicable — Editor-only package |

---

## Tier overview

| Tier | In this repo |
|------|----------------|
| **Base** (free) | Yes |
| **Core / Pro / Studio** | No — distributed separately |

---

## License

EditorMCP is **proprietary** software. See **`package/com.datumstudios.editormcp.base/LICENSE.txt`**. Third-party notices: **`package/com.datumstudios.editormcp.base/ThirdPartyLicenses.md`**.
