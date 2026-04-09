# EditorMCP

**EditorMCP** is a Unity Editor package that hosts an MCP (Model Context Protocol) server so MCP-capable clients can work with your project through a structured, editor-native integration.

If you use AI assistants or automation that speak MCP, EditorMCP connects them to Unity without shipping player/runtime code in this distribution.

EditorMCP is a Unity-hosted MCP automation platform that enables deterministic, structured interaction with your project through a curated tool surface.

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

## Tier Overview

| Tier | Availability |
|------|----------------|
| **Base (Free)** | Included in this repository |
| **Core** | Unity Asset Store (pending publication) |
| **Pro** | Unity Asset Store (pending publication) |
| **Studio** | Unity Asset Store (pending publication) |

---

## 📦 Availability

EditorMCP has been submitted to the Unity Asset Store and is currently under review.

The Base tier is publicly available in this repository (proprietary license).  
Core, Pro, and Studio are distributed as packaged Unity assets.

Store links will be added here once the packages are approved and published.

---

## Why EditorMCP Exists

Most MCP-style integrations focus on connectivity — exposing Unity functionality to external tools — but fall short in production use.

Common issues include:

- Unstable connections and dropped sessions
- Weak lifecycle handling (reloads, domain resets, play mode transitions)
- Unstructured or inconsistent tool responses
- Lack of safety boundaries for mutating operations
- Minimal observability and debugging support

These limitations make them difficult to rely on for real workflows.

### What Makes EditorMCP Different

EditorMCP is designed as a production-ready system, not just a bridge.

- **Editor-hosted runtime** — Runs directly inside Unity — no external bridge layer or fragile process syncing
- **Deterministic tool execution** — Structured inputs and outputs using a consistent ToolResult contract
- **Lifecycle-aware architecture** — Handles domain reloads, scene changes, and editor state transitions reliably
- **Explicit safety model** — Clear boundaries between read-only, targeted mutation, and higher-scope operations
- **Curated tool surface** — 130 tools designed, tested, and validated — not ad-hoc method exposure
- **Built-in observability** — Logging, diagnostics, and error visibility designed for real debugging

### Why This Matters

Instead of:

- writing one-off editor scripts
- debugging fragile integrations
- dealing with inconsistent tool behavior

You get:

- a structured, reliable automation layer inside Unity

---

## License

EditorMCP is **proprietary** software. See **`package/com.datumstudios.editormcp.base/LICENSE.txt`**. Third-party notices: **`package/com.datumstudios.editormcp.base/ThirdPartyLicenses.md`**.
