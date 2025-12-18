# EditorMCP Core Tools v0.1

This document defines the canonical list of 15 read-only tools for EditorMCP Core v0.1. All tools in this version are **read-only** and designed for safe inspection and analysis of Unity projects.

## Tool Categories

- **MCP Platform Tools** (3): Core MCP protocol and server information
- **Project & Environment** (4): Project-wide inspection and validation
- **Scene & Hierarchy** (3): Scene structure and component inspection
- **Asset Inspection** (2): Asset metadata and dependency analysis
- **Audio** (2): AudioMixer inspection (example domain)
- **Editor State** (1): Current editor selection context

---

## MCP Platform Tools

### mcp.server.info

**Purpose**: Returns server and environment information to verify the MCP bridge is operational and provide context for tool execution.

**Inputs**:
- None

**Outputs**:
```json
{
  "serverVersion": "string",
  "unityVersion": "string",
  "platform": "string",
  "enabledToolCategories": ["string"],
  "tier": "string"
}
```

**Safety Note**: Read-only. Returns metadata only; no project state is modified.

---

### mcp.tools.list

**Purpose**: Lists all available tools with their metadata, categories, and tier availability. Required for MCP client discovery.

**Inputs**:
- `category` (optional, string): Filter by tool category
- `tier` (optional, string): Filter by tier availability

**Outputs**:
```json
{
  "tools": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "category": "string",
      "safetyLevel": "read-only",
      "tier": "string"
    }
  ]
}
```

**Safety Note**: Read-only. Returns tool metadata only; does not execute any tools.

---

### mcp.tool.describe

**Purpose**: Returns the complete schema for a specific tool, including input parameters, output structure, and safety information. Critical for LLM grounding and human inspection.

**Inputs**:
- `toolId` (required, string): The ID of the tool to describe

**Outputs**:
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "category": "string",
  "safetyLevel": "read-only",
  "tier": "string",
  "inputs": {
    "propertyName": {
      "type": "string",
      "required": true,
      "description": "string"
    }
  },
  "outputs": {
    "propertyName": {
      "type": "string",
      "description": "string"
    }
  },
  "notes": "string"
}
```

**Safety Note**: Read-only. Returns tool schema only; does not execute the tool.

---

## Project & Environment Tools

### project.info

**Purpose**: Returns high-level project information including Unity version, render pipeline, build targets, and project configuration. Provides foundational context for all other operations.

**Inputs**:
- None

**Outputs**:
```json
{
  "projectName": "string",
  "unityVersion": "string",
  "renderPipeline": "string",
  "buildTargets": ["string"],
  "projectPath": "string"
}
```

**Safety Note**: Read-only. Reads project settings only; no modifications are made.

---

### project.scenes.list

**Purpose**: Lists all scenes in the project with their paths and build settings status. Provides foundational context for scene-based operations.

**Inputs**:
- `includeInBuild` (optional, boolean): Filter by scenes included in build settings

**Outputs**:
```json
{
  "scenes": [
    {
      "path": "string",
      "name": "string",
      "enabledInBuild": true,
      "buildIndex": 0
    }
  ]
}
```

**Safety Note**: Read-only. Reads scene list from EditorBuildSettings; no scene files are modified.

---

### project.assets.summary

**Purpose**: Returns a summary of project assets including counts by type, large assets, and unreferenced asset detection. Provides project health insights without mutation.

**Inputs**:
- `assetType` (optional, string): Filter by asset type (e.g., "Texture2D", "AudioClip")
- `minSizeBytes` (optional, integer): Filter assets larger than this size

**Outputs**:
```json
{
  "totalAssets": 0,
  "byType": {
    "assetType": 0
  },
  "largeAssets": [
    {
      "path": "string",
      "type": "string",
      "sizeBytes": 0
    }
  ],
  "unreferencedCount": 0
}
```

**Safety Note**: Read-only. Analyzes asset database only; no assets are modified or deleted.

---

### project.references.missing

**Purpose**: Detects missing script references and broken asset references in the project. High-value diagnostic tool with zero write risk.

**Inputs**:
- `scope` (optional, string): "all" | "scenes" | "prefabs" | "assets"

**Outputs**:
```json
{
  "missingScripts": [
    {
      "path": "string",
      "componentIndex": 0,
      "guid": "string"
    }
  ],
  "brokenReferences": [
    {
      "path": "string",
      "referencePath": "string",
      "referenceGuid": "string"
    }
  ]
}
```

**Safety Note**: Read-only. Scans for missing references only; does not attempt to fix or modify any assets.

---

## Scene & Hierarchy Tools

### scene.hierarchy.dump

**Purpose**: Returns the complete GameObject hierarchy for a scene, including components per node and object paths. Cornerstone tool for editor reasoning and structural analysis.

**Inputs**:
- `scenePath` (required, string): Path to the scene file (e.g., "Assets/Scenes/Main.unity")

**Outputs**:
```json
{
  "scenePath": "string",
  "rootObjects": [
    {
      "name": "string",
      "path": "string",
      "instanceId": 0,
      "components": ["string"],
      "children": []
    }
  ]
}
```

**Safety Note**: Read-only. Reads scene hierarchy only; no GameObjects or components are modified.

---

### scene.objects.find

**Purpose**: Finds GameObjects in a scene matching specified criteria (component type, name pattern, tag, layer). Composable and extremely useful for targeted inspection.

**Inputs**:
- `scenePath` (required, string): Path to the scene file
- `componentType` (optional, string): Filter by component type name
- `namePattern` (optional, string): Filter by GameObject name (supports wildcards)
- `tag` (optional, string): Filter by tag
- `layer` (optional, integer): Filter by layer index

**Outputs**:
```json
{
  "matches": [
    {
      "name": "string",
      "path": "string",
      "instanceId": 0,
      "components": ["string"],
      "tag": "string",
      "layer": 0
    }
  ]
}
```

**Safety Note**: Read-only. Searches scene hierarchy only; no objects are selected or modified.

---

### scene.components.list

**Purpose**: Returns all components attached to a specific GameObject, including serialized field names. Enables safe inspection before any potential edits.

**Inputs**:
- `scenePath` (required, string): Path to the scene file
- `gameObjectPath` (required, string): Hierarchy path to the GameObject (e.g., "Canvas/Panel/Button")

**Outputs**:
```json
{
  "gameObjectPath": "string",
  "components": [
    {
      "type": "string",
      "instanceId": 0,
      "serializedFields": [
        {
          "name": "string",
          "type": "string",
          "value": "any"
        }
      ]
    }
  ]
}
```

**Safety Note**: Read-only. Reads component data only; no components or properties are modified.

---

## Asset Inspection Tools

### asset.info

**Purpose**: Returns detailed information about a specific asset including type, dependencies, and import settings. Demonstrates asset graph awareness without mutation.

**Inputs**:
- `assetPath` (required, string): Path to the asset (e.g., "Assets/Textures/Logo.png")
- `includeDependencies` (optional, boolean): Include dependency list

**Outputs**:
```json
{
  "path": "string",
  "guid": "string",
  "type": "string",
  "sizeBytes": 0,
  "dependencies": ["string"],
  "importSettings": {}
}
```

**Safety Note**: Read-only. Reads asset metadata and import settings only; no asset files or import settings are modified.

---

### asset.dependencies.graph

**Purpose**: Returns the dependency graph for an asset (what it depends on and what depends on it). Useful for understanding asset relationships and impact analysis.

**Inputs**:
- `assetPath` (required, string): Path to the asset
- `depth` (optional, integer): Maximum depth to traverse (default: 2)
- `direction` (optional, string): "dependencies" | "dependents" | "both" (default: "both")

**Outputs**:
```json
{
  "assetPath": "string",
  "dependencies": [
    {
      "path": "string",
      "type": "string",
      "depth": 0
    }
  ],
  "dependents": [
    {
      "path": "string",
      "type": "string",
      "depth": 0
    }
  ]
}
```

**Safety Note**: Read-only. Analyzes asset dependency graph only; no assets are modified or moved.

---

## Audio Tools (Example Domain)

### audio.mixer.list

**Purpose**: Lists all AudioMixer assets in the project with their groups and snapshots. Concrete, familiar example of domain-specific tooling without mutation.

**Inputs**:
- None

**Outputs**:
```json
{
  "mixers": [
    {
      "path": "string",
      "name": "string",
      "groups": [
        {
          "name": "string",
          "path": "string"
        }
      ],
      "snapshots": [
        {
          "name": "string"
        }
      ]
    }
  ]
}
```

**Safety Note**: Read-only. Reads AudioMixer asset structure only; no mixer settings or snapshots are modified.

---

### audio.mixer.snapshot.read

**Purpose**: Returns the parameter values for a specific AudioMixer snapshot. Shows structured, numeric tooling without mutation.

**Inputs**:
- `mixerPath` (required, string): Path to the AudioMixer asset
- `snapshotName` (required, string): Name of the snapshot to read

**Outputs**:
```json
{
  "mixerPath": "string",
  "snapshotName": "string",
  "parameters": [
    {
      "name": "string",
      "value": 0.0,
      "group": "string"
    }
  ]
}
```

**Safety Note**: Read-only. Reads snapshot parameter values only; no mixer parameters or snapshots are modified.

---

## Editor State Tools

### editor.selection.info

**Purpose**: Returns information about currently selected objects and assets in the Unity Editor. Bridges human and AI workflows by providing current editor context.

**Inputs**:
- None

**Outputs**:
```json
{
  "selectedObjects": [
    {
      "name": "string",
      "path": "string",
      "type": "string",
      "instanceId": 0
    }
  ],
  "activeScene": "string",
  "activeGameObject": {
    "name": "string",
    "path": "string"
  }
}
```

**Safety Note**: Read-only. Reads current editor selection state only; selection is not modified.

---

## Version Information

- **Spec Version**: 0.1.0
- **EditorMCP Core Version**: 0.1.0
- **Last Updated**: 2024-01-01

## Notes

All tools in v0.1 are **read-only**. This ensures:
- Zero risk of accidental project modification
- Safe exploration and inspection workflows
- Clear upgrade path to write-capable tools in future versions
- Trust-building through demonstrated safety

Future versions may introduce write-capable tools with appropriate safety guardrails and tier restrictions.

## Performance Bounds and Best-Effort Behavior

### Time Guards

Several tools perform scanning operations that may take significant time on large projects. To prevent hangs and ensure responsive behavior, these tools implement time guards:

- **`project.references.missing`**: Maximum 15 seconds for scene/prefab scanning. Processes up to 100 scenes and 200 prefabs per scan. Results may be partial if time limits are exceeded.
- **`project.assets.summary`**: Maximum 10 seconds for asset type counting. Results may be partial if time limits are exceeded.

When time limits are exceeded, tools return partial results with diagnostic messages in the `diagnostics` field of the response.

### Deterministic Ordering

All tool outputs are designed to be deterministic:
- Arrays are sorted by stable keys (e.g., name, path, ID)
- Dictionary keys are sorted alphabetically where applicable
- This ensures consistent JSON output across invocations, which is critical for LLM tool use and testing

### Best-Effort Limitations

Some tools operate with "best-effort" limitations due to Unity API constraints:

- **`project.assets.summary`**: Timeline detection may return 0 if Timeline package is not installed
- **`project.references.missing`**: May not detect all missing references in all asset types; focuses on scenes and prefabs
- **`audio.mixer.snapshot.read`**: Parameter reading uses reflection-based workarounds due to Unity API limitations; some parameters may not be accessible

These limitations are documented in tool-specific notes and communicated via the `diagnostics` field when applicable.

