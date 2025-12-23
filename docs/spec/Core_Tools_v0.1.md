# EditorMCP Core Tools v0.1

This document defines the canonical list of 18 tools for EditorMCP Core v0.1. Most tools are **read-only** and designed for safe inspection and analysis of Unity projects. A small number of safe single-object write operations are included for basic hierarchy management.

## Tool Categories

- **MCP Platform Tools** (3): Core MCP protocol and server information
- **Project & Environment** (4): Project-wide inspection and validation
- **Scene & Hierarchy** (3): Scene structure and component inspection
- **GameObject Operations** (3): GameObject discovery and safe hierarchy management
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
  "success": true,
  "serverVersion": "1.0.0",
  "unityVersion": "2022.3.20f1",
  "minUnityVersion": "2022.3.0f1",
  "isCompatible": true,
  "platform": "WindowsEditor",
  "enabledToolCategories": ["mcp", "project", "scene", "go", "asset", "audio", "editor"],
  "tier": "core",
  "toolCount": 18
}
```

**Output Fields:**
- `success` (boolean): Always `true` if server is running
- `serverVersion` (string): EditorMCP package version (e.g., "1.0.0")
- `unityVersion` (string): Current Unity version (e.g., "2022.3.20f1")
- `minUnityVersion` (string): Minimum required Unity version (e.g., "2022.3.0f1")
- `isCompatible` (boolean): `true` if current Unity version meets minimum requirement
- `platform` (string): Unity platform identifier (e.g., "WindowsEditor", "MacEditor", "LinuxEditor")
- `enabledToolCategories` (array of strings): List of tool categories available
- `tier` (string): Current license tier ("core", "pro", "studio", "enterprise")
- `toolCount` (integer): Total number of tools available at current tier

**Implementation:**
```csharp
[McpTool("mcp.server.info")]
public static string ServerInfo() {
    return JsonUtility.ToJson(new {
        success = true,
        serverVersion = "1.0.0",
        unityVersion = Application.unityVersion,
        minUnityVersion = VersionValidator.GetMinimumVersion(),
        isCompatible = VersionValidator.IsCompatible(),
        platform = Application.platform.ToString(),
        enabledToolCategories = ToolRegistry.GetCategories(),
        tier = LicenseManager.CurrentTier.ToString().ToLower(),
        toolCount = ToolRegistry.ToolCount
    });
}
```

**Buyer Value:**
- Cursor/Claude can check `isCompatible` → provides "Upgrade Unity" guidance if false
- Version information helps diagnose compatibility issues
- Tool count indicates tier availability

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

## GameObject Operations Tools

### go.find

**Purpose**: Finds GameObjects in the active scene matching specified criteria (hierarchy path pattern, component type, tag, layer). Essential discovery tool that enables 90% of Cursor queries starting with "find objects". Works on the currently active scene without requiring scene path specification.

**Inputs**:
- `hierarchyPath` (optional, string): Hierarchy path pattern with wildcards (e.g., "Root/Enemies/*", "Root/**/Enemy")
  - `*` = direct children only (matches single path segment: `[^/]*`)
  - `**` = recursive descendants (matches zero or more path segments: `.*?`)
  - Examples: `"Root/Enemies/*"` matches `Root/Enemies/Goblin`, `Root/Enemies/Orc` (direct children)
  - Examples: `"Root/**/Enemy"` matches `Root/Enemies/Goblin/Enemy`, `Root/Enemies/Orc/Enemy` (any descendant)
- `componentType` (optional, string): Filter by component type name
  - Accepts: `"BoxCollider"` or `"UnityEngine.BoxCollider"` (both formats supported)
  - Output always uses full namespace: `"UnityEngine.BoxCollider"`
- `tag` (optional, string): Filter by tag name (e.g., "Enemy")
- `layer` (optional, integer): Filter by layer index
- `namePattern` (optional, string): Filter by GameObject name pattern (supports wildcards, e.g., "Gob*")
- `maxResults` (optional, integer): Maximum number of results to return (default: 100, performance safeguard)

**Outputs**:
```json
{
  "matches": [
    {
      "instanceId": 12345,
      "name": "Goblin",
      "hierarchyPath": "Root/Enemies/Goblin",
      "components": ["UnityEngine.Transform", "UnityEngine.BoxCollider"],
      "tag": "Enemy",
      "layer": 8
    }
  ],
  "activeScene": "Assets/Scenes/Main.unity"
}
```

**Validation Rules**:
- Requires an active scene to be loaded (uses `EditorSceneManager.GetActiveScene()`, no `scenePath` parameter)
- Hierarchy path patterns support `*` (single level) and `**` (recursive) wildcards
- Wildcard algorithm: Convert glob to regex (`*` → `[^/]*`, `**` → `.*?`)
- All filters are optional; multiple filters combine with AND logic
- Results are sorted by hierarchy path for deterministic output
- Performance: Limited by `maxResults` parameter (default: 100)

**Error Handling**:
- No active scene: `success = false`, `error = "No active scene loaded"`
- Invalid hierarchy path pattern: `success = false`, `error = "Invalid hierarchy path pattern"`

**Safety Note**: Read-only discovery operation. Searches active scene hierarchy only; no objects are selected, modified, or created. Universal debugging/inspection tool used daily by all developers.

**Implementation Notes:**
- Uses `HierarchyResolver.FindByPath()` for path resolution
- Uses `Regex.IsMatch()` with glob-to-regex conversion for wildcard matching
- Component type resolution: Try `Type.GetType(componentType)` first, then `Type.GetType($"UnityEngine.{componentType}, UnityEngine")`
- JSON serialization: `UnityEngine.JsonUtility` (Unity 2022.3 native)

---

### go.setParent

**Purpose**: Changes the parent of a GameObject in the active scene. Enables basic hierarchy management for single-object operations. Essential for "reparent this" workflows that indies use constantly.

**Inputs**:
- `childPath` (required, string): Hierarchy path to the GameObject to reparent (e.g., "Root/Enemies/Goblin")
- `parentPath` (optional, string): Hierarchy path to the new parent GameObject (e.g., "Root/Enemies/Container"). If omitted or empty string, reparents to scene root.

**Outputs**:
```json
{
  "success": true,
  "childPath": "Root/Enemies/Goblin",
  "newParentPath": "Root/Enemies/Container",
  "newHierarchyPath": "Root/Enemies/Container/Goblin"
}
```

**Validation Rules**:
- Child GameObject must exist in active scene
- Parent GameObject must exist in active scene (if specified)
- Cannot set parent to a descendant of the child (prevents circular hierarchy)
- Requires an active scene to be loaded

**Error Handling**:
- No active scene: `success = false`, `error = "No active scene loaded"`
- Child not found: `success = false`, `error = "Child GameObject not found"`
- Parent not found: `success = false`, `error = "Parent GameObject not found"`
- Circular hierarchy: `success = false`, `error = "Cannot set parent to descendant"`

**Safety Note**: Single-object hierarchy modification. Uses `Undo.SetTransformParent(transform, newParent, "go.setParent")` (Unity recommended API) with `UndoScope` pattern for undo support. Low-risk operation that indies need constantly for basic hierarchy management. Not a batch operation.

**Implementation Notes:**
- Unity API: `Undo.SetTransformParent(transform, newParent, "EditorMCP: Set Parent")`
- Uses `UndoScope` pattern: `using var undo = new UndoScope("go.setParent");`
- Circular hierarchy detection: Traverse up from potential parent to root, check if child is encountered
- Scene root reparenting: When `parentPath` is empty/null, `parent = null` (reparent to scene root)
- Uses `HierarchyResolver.FindByPath()` for path resolution
- Active scene only: Uses `EditorSceneManager.GetActiveScene()`, no `scenePath` parameter
- JSON serialization: `UnityEngine.JsonUtility` (Unity 2022.3 native)

---

### component.list

**Purpose**: Returns all components attached to a GameObject in the active scene, including component types and property counts. Perfect complement to `go.find` for "What components on this?" queries. Works on the currently active scene without requiring scene path specification.

**Inputs**:
- `hierarchyPath` (required, string): Hierarchy path to the GameObject (e.g., "Root/Enemies/Goblin")
- `includeProperties` (optional, boolean): Include serialized field names and types (default: false)
  - When `false`: Only returns `propertyCount` per component
  - When `true`: Includes top-level serialized fields only (no nesting, max 50 properties per component)

**Outputs** (includeProperties=false, default):
```json
{
  "success": true,
  "hierarchyPath": "Root/Enemies/Goblin",
  "components": [
    {
      "type": "UnityEngine.BoxCollider",
      "instanceId": 67890,
      "propertyCount": 8
    },
    {
      "type": "UnityEngine.Rigidbody",
      "instanceId": 67891,
      "propertyCount": 12
    }
  ],
  "activeScene": "Assets/Scenes/Main.unity"
}
```

**Outputs** (includeProperties=true):
```json
{
  "success": true,
  "hierarchyPath": "Root/Enemies/Goblin",
  "components": [
    {
      "type": "UnityEngine.BoxCollider",
      "instanceId": 67890,
      "propertyCount": 8,
      "serializedFields": [
        {"name": "m_Size", "type": "Vector3", "value": {"x": 1, "y": 1, "z": 1}},
        {"name": "m_Center", "type": "Vector3", "value": {"x": 0, "y": 0, "z": 0}},
        {"name": "m_IsTrigger", "type": "boolean", "value": false}
      ]
    }
  ],
  "activeScene": "Assets/Scenes/Main.unity"
}
```

**Validation Rules**:
- Requires an active scene to be loaded
- GameObject must exist in active scene

**Error Handling**:
- No active scene: `success = false`, `error = "No active scene loaded"`
- GameObject not found: `success = false`, `error = "GameObject not found"`

**Safety Note**: Read-only component inspection. Reads component data from active scene only; no components or properties are modified. Universal debugging tool that complements `go.find` for discovery workflows.

**Implementation Notes:**
- Uses `HierarchyResolver.FindByPath()` for GameObject lookup
- Property serialization: Uses `SerializedObject` for property access (top-level only, no nesting)
- Property limit: Max 50 properties per component (performance safeguard)
- Default: `includeProperties = false` (Core tier simplicity)
- Active scene only: Uses `EditorSceneManager.GetActiveScene()`, no `scenePath` parameter
- JSON serialization: `UnityEngine.JsonUtility` (Unity 2022.3 native)

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

Most tools in v0.1 are **read-only** to ensure:
- Zero risk of accidental project modification
- Safe exploration and inspection workflows
- Clear upgrade path to write-capable tools in future versions
- Trust-building through demonstrated safety

**Exception: `go.setParent`** is included as a safe single-object write operation because:
- It's a universal need for basic hierarchy management
- It's undo-safe and low-risk (single object, no batch)
- It enables "try before buy" evaluation of write capabilities
- It matches the Core philosophy of "discovery + basic fixes"

Future versions may introduce additional write-capable tools with appropriate safety guardrails and tier restrictions.

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

---

## Version Compatibility

### Version Strategy Summary

- **Minimum Unity Version**: Unity 2022.3 LTS (Asset Store requirement)
- **Target Versions**: Unity 2022.3 LTS + Unity 6.0 LTS
- **Compatibility Status**: All 18 Core tools fully compatible 2022.3 LTS → Unity 6.0 LTS with zero conditional compilation needed

### Core Tools Compatibility Matrix (18 Tools)

| Tool | Unity 2022.3 LTS | Unity 6.0 LTS | Version Notes |
|------|------------------|---------------|---------------|
| **MCP Platform Tools** |
| `mcp.server.info` | ✅ Full | ✅ Full | `Application.unityVersion` stable |
| `mcp.tools.list` | ✅ Full | ✅ Full | ToolRegistry discovery unchanged |
| `mcp.tool.describe` | ✅ Full | ✅ Full | Schema generation unchanged |
| **Project & Environment** |
| `project.info` | ✅ Full | ✅ Full | `AssetDatabase` APIs stable |
| `project.scenes.list` | ✅ Full | ✅ Full | `EditorBuildSettings` stable |
| `project.assets.summary` | ✅ Full | ✅ Full | `AssetDatabase.FindAssets()` stable |
| `project.references.missing` | ✅ Full | ✅ Full | GUID system stable |
| **Scene & Hierarchy** |
| `scene.hierarchy.dump` | ✅ Full | ✅ Full | `GameObject` hierarchy APIs unchanged |
| `scene.objects.find` | ✅ Full | ✅ Full | `GameObject.Find()` stable |
| `scene.components.list` | ✅ Full | ✅ Full | `Component` APIs unchanged |
| **GameObject Operations** |
| `go.find` | ✅ Full | ✅ Full | `GameObject.Find()` stable |
| `go.setParent` | ✅ Full | ✅ Full | `Transform.SetParent()` unchanged |
| `component.list` | ✅ Full | ✅ Full | `GetComponents()` unchanged |
| **Asset Inspection** |
| `asset.info` | ✅ Full | ✅ Full | `AssetDatabase` APIs stable |
| `asset.dependencies.graph` | ✅ Full | ✅ Full | `AssetDatabase.GetDependencies()` stable |
| **Audio** |
| `audio.mixer.list` | ✅ Full | ✅ Full | `AssetDatabase.FindAssets()` stable |
| `audio.mixer.snapshot.read` | ✅ Full | ✅ Full | AudioMixer APIs unchanged |
| **Editor State** |
| `editor.selection.info` | ✅ Full | ✅ Full | `Selection` APIs unchanged |

**Legend:**
- ✅ Full = Complete feature parity, no version differences

### Version Notes

**Status**: All 18 Core tools have zero conditional compilation needed. All Unity APIs used by Core tools are stable since Unity 2017+ and unchanged across 2022.3 → Unity 6.0.

**Core Infrastructure Compatibility:**
- `ToolRegistry`: Attribute-driven discovery works identically on 2022.3 and Unity 6.0
- `AssetDatabase`: All Core tool APIs stable since Unity 2017.1
- `GameObject`/`Component`: No API changes affecting Core tools
- `Selection`: Editor selection APIs unchanged

**Package Requirements:**
- **None** - All Core tools use only built-in Unity APIs
- No package dependencies required for Core tier

**Testing Recommendations:**
- Unity 2022.3.20f1 LTS (minimum version)
- Unity 6000.0.0f1 LTS (latest Unity 6)

---

## Implementation Infrastructure

### HierarchyResolver (Shared Helper)

**Location:** `Editor/EditorMcp/Helpers.cs` (shared across all tiers)

**Purpose:** Provides consistent hierarchy path resolution for all tools.

**Methods:**
- `FindByPath(string path)` - Finds GameObject by hierarchy path in active scene
- `GetFullPath(this Transform transform)` - Gets full hierarchy path for a Transform

**Implementation:**
```csharp
public static class HierarchyResolver {
    public static GameObject FindByPath(string path) {
        if (string.IsNullOrEmpty(path)) return null;
        
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded) return null;
        
        var pathParts = path.Split('/');
        if (pathParts.Length == 0) return null;
        
        // Find root GameObject
        var rootGo = scene.GetRootGameObjects()
            .FirstOrDefault(go => go.name == pathParts[0]);
        if (rootGo == null) return null;
        
        // Navigate down hierarchy using Transform.Find()
        Transform current = rootGo.transform;
        for (int i = 1; i < pathParts.Length; i++) {
            current = current.Find(pathParts[i]);
            if (current == null) return null;
        }
        
        return current.gameObject;
    }
    
    public static string GetFullPath(this Transform transform) {
        if (transform == null) return "";
        
        var path = new List<string>();
        var current = transform;
        while (current != null) {
            path.Add(current.name);
            current = current.parent;
        }
        path.Reverse();
        return string.Join("/", path);
    }
}
```

**Usage:**
- Used by `go.find`, `go.setParent`, `component.list` (Core tier)
- Used by future Pro/Studio/Enterprise tools
- Single implementation ensures consistency across all tiers

### Wildcard Pattern Matching

**Algorithm:** Glob-to-Regex conversion (Unity QuickSearch pattern)

**Pattern Rules:**
- `*` → `[^/]*` (matches single path segment - direct children only)
- `**` → `.*?` (matches zero or more path segments - recursive descendants)
- `?` → `.` (single character - optional, for future expansion)

**Conversion Function:**
```csharp
private static string ConvertGlobToRegex(string glob) {
    if (string.IsNullOrEmpty(glob)) return null;
    return "^" + Regex.Escape(glob)
        .Replace(@"\*\*", ".*?")      // ** → recursive (non-greedy)
        .Replace(@"\*", @"[^/]*")     // * → direct children (single segment)
        .Replace(@"\\", @"\") + "$";
}
```

**Examples:**
- `"Root/Enemies/*"` → Matches: `Root/Enemies/Goblin`, `Root/Enemies/Orc` (direct children)
- `"Root/**/Enemy"` → Matches: `Root/Enemies/Goblin/Enemy`, `Root/Enemies/Orc/Enemy` (any descendant)
- `"Root/Enemies/Gob*"` → Matches: `Root/Enemies/Goblin`, `Root/Enemies/GoblinKing` (name pattern)

### Component Type Resolution

**Input Format Support:**
- Short name: `"BoxCollider"` → Resolved to `UnityEngine.BoxCollider`
- Full name: `"UnityEngine.BoxCollider"` → Direct match

**Resolution Algorithm:**
```csharp
private static Type ResolveComponentType(string typeName) {
    // Try direct match first
    var type = Type.GetType(typeName);
    if (type != null && type.IsSubclassOf(typeof(Component)))
        return type;
    
    // Try with UnityEngine namespace
    type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
    if (type != null && type.IsSubclassOf(typeof(Component)))
        return type;
    
    return null;
}
```

**Output Format:**
- Always use full namespace: `"UnityEngine.BoxCollider"`
- Consistent with Unity's type system
- Matches existing tool outputs

### Active Scene vs Scene Path

**Core Tools (Active Scene Only):**
- `go.find` - Works ONLY on `EditorSceneManager.GetActiveScene()`
- `go.setParent` - Works ONLY on active scene
- `component.list` - Works ONLY on active scene
- **No `scenePath` parameter** - Simpler API, faster execution, common use case

**Scene Tools (Scene Path Parameter):**
- `scene.objects.find` - Accepts `scenePath` parameter (can open scenes additively)
- `scene.components.list` - Accepts `scenePath` parameter
- `scene.hierarchy.dump` - Accepts `scenePath` parameter
- **Explicit scene path** - For batch operations and scene management

**Error Handling:**
- If no active scene: `{"success": false, "error": "No active scene loaded. Open a scene first."}`
- Use `EditorSceneManager.GetActiveScene().isLoaded` to check

### JSON Serialization

**Library:** `UnityEngine.JsonUtility` (Unity 2022.3+ native, no dependencies)

**Usage:**
- `JsonUtility.FromJson<T>(json)` - Deserialize input
- `JsonUtility.ToJson(object)` - Serialize output
- Standard Unity pattern, no external dependencies

**Error Response Format:**
```json
{
  "success": false,
  "error": "Error message here",
  "code": "ERROR_CODE"
}
```

**Success Response Format:**
```json
{
  "success": true,
  // ... tool-specific output
}
```

