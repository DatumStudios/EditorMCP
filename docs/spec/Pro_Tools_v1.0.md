# EditorMCP Pro Tools v1.0

This document defines the Pro tier tools for EditorMCP v1.0. Pro tier tools enable safe, single-object/single-asset write operations for individual developers. All Pro tools are designed for focused, low-blast-radius operations that modify project state safely.

## Tool Categories

- **Project & File Operations** (6): File system operations, text search, and script inspection
- **GameObject & Component Operations** (6): Create, modify, and delete GameObjects and components
- **Prefab Operations** (1): Single-prefab property editing
- **ScriptableObject Operations** (1): Single-asset ScriptableObject field editing
- **Audio Operations** (3): AudioMixer group, snapshot, and routing operations
- **Timeline Operations** (3): Timeline track creation, object binding, and marker operations
- **UI Toolkit Operations** (3): UXML document creation, style management, and data binding
- **Editor Operations** (3): Safe menu command execution and selection operations
- **Unity API & Package Search** (2): Unity documentation and package registry search
- **Batch Operations** (1): Limited-scale batch property setting

## File Organization

Pro tier tools are organized into category files under the unified `Editor/EditorMcp/Tools/` directory. These files are **shared across tiers** with tier gating via `[McpTool]` attribute:

- **ProjectTools.cs** – `project.getVersion` (Pro)
- **FileTools.cs** – `project.listFiles`, `project.readFile`, `project.writeFile`, `project.searchText`, `project.inspectScript` (Pro)
- **GameObjectTools.cs** – `go.*`, `component.*` tools (Pro)
- **PrefabTools.cs** – `prefab.*` tools - **SHARED**: Pro (prefab.editProperty) + Studio (prefab.batchReplace)
- **ScriptableObjectTools.cs** – `sobj.*` tools (Pro/Studio)
- **AudioTools.cs** – `audio.mixer.*` tools - **SHARED**: Pro (audio.mixer.createGroup) + Studio (audio.mixer.batchRoute)
- **TimelineTools.cs** – `timeline.*` tools - **SHARED**: Pro (timeline.createTrack) + Studio (timeline.sectionMarkers)
- **UIToolkitTools.cs** – `ui.createDocument`, `ui.addStyle`, `ui.bindData` (Pro)
- **EditorTools.cs** – `editor.executeMenu`, `editor.selection.*` (Pro)
- **UnityApiTools.cs** – `unityApi.*`, `unityPackages.*` (Pro)
- **BatchTools.cs** – `batch.*` tools - **SHARED**: Pro (10 items) + Studio (50 items) + Enterprise (500 items)

**Shared Category Files Pattern:**
- `PrefabTools.cs`, `AudioTools.cs`, `TimelineTools.cs`, and `BatchTools.cs` contain tools from multiple tiers
- Tier gating is enforced via `[McpTool("tool.id", tier: Tier.Pro)]` attribute
- This matches Asset Store best practices (Odin, PlayMaker pattern)

Each tool is implemented as a static method with `[McpTool("tool.id")]` attribute and uses request/response DTOs.

---

## Project & File Operations Tools

### project.getVersion

**Purpose**: Returns Unity editor version and project version information. Provides version context for compatibility checks and project metadata.

**File**: `ProjectTools.cs`

**Unity APIs**: `Application.version` (player version), `ProjectVersion.txt` for Editor version

**Inputs**:
- None

**Outputs**:
```json
{
  "success": true,
  "unityVersion": "2022.3.20f1",
  "projectVersion": "0.1.0",
  "scriptingRuntime": "NET_Standard_2_1"
}
```

**Implementation Notes**:
- Reads `ProjectSettings/ProjectVersion.txt` and parses `m_EditorVersion` line
- Uses `Application.version` for player/project version (from `PlayerSettings.bundleVersion`)
- Returns scripting runtime version
- Uses `PlayerSettings.GetScriptingBackend()` for scripting runtime detection

**Error Handling**:
- If `ProjectVersion.txt` is missing: `success = false`, `error = "ProjectVersion.txt not found"`

**Safety Note**: Read-only. Returns version metadata only; no project state is modified.

---

### project.listFiles

**Purpose**: Lists files in the project matching a glob pattern. Essential for code generation, refactoring, and file-based automation workflows.

**File**: `FileTools.cs`

**Unity APIs**: `System.IO.Directory.GetFiles`, `System.IO.EnumerationOptions`

**Inputs**:
```json
{
  "root": "Assets/",
  "pattern": "**/*.cs",
  "maxResults": 500
}
```

- `root` (required, string): Root directory path. Must start with `Assets/` and not include `Packages/`
- `pattern` (required, string): Glob pattern supporting `*`, `?`, `**/` style wildcards
- `maxResults` (optional, integer): Maximum number of results to return (default: 500)

**Outputs**:
```json
{
  "success": true,
  "root": "Assets/",
  "pattern": "**/*.cs",
  "files": [
    "Assets/Scripts/MyScript.cs",
    "Assets/Scripts/Editor/Tool.cs"
  ],
  "truncated": false
}
```

**Validation Rules**:
- `root` must pass `SafePaths.IsAssetsSafe(root)` validation
- Reject paths containing `../` segments
- Pattern is translated to `Directory.GetFiles` compatible format

**Error Handling**:
- Invalid root path: `success = false`, `error = "Root path must be under Assets/"`
- Pattern compilation failure: `success = false`, `error = "Invalid glob pattern"`

**Safety Note**: Read-only file listing. No files are modified or deleted.

---

### project.readFile

**Purpose**: Reads file content from the project. Essential for code generation, refactoring, and PRD-driven config edits.

**File**: `FileTools.cs`

**Unity APIs**: `System.IO.File.ReadAllText`, `System.IO.File.Exists`

**Inputs**:
```json
{
  "path": "Assets/MyConfig.json",
  "maxBytes": 65536
}
```

- `path` (required, string): File path relative to project root (must be under `Assets/`)
- `maxBytes` (optional, integer): Maximum file size to read (default: 65536 bytes)

**Outputs**:
```json
{
  "success": true,
  "path": "Assets/MyConfig.json",
  "encoding": "utf-8",
  "content": "{ \"foo\": 1 }"
}
```

**Validation Rules**:
- Path must pass `SafePaths.IsAssetsSafe(path)` validation
- File size must not exceed `maxBytes` (prevents memory/time issues)
- Only files under `Assets/` are accessible

**Error Handling**:
- Invalid path: `success = false`, `error = "Path must be under Assets/"`
- File not found: `success = false`, `error = "File not found"`
- File too large: `success = false`, `error = "File exceeds maxBytes limit"`

**Safety Note**: Read-only. File content is returned but not modified.

---

### project.writeFile

**Purpose**: Writes file content to the project. Enables safe file creation and modification for code generation and configuration updates.

**File**: `FileTools.cs`

**Unity APIs**: `System.IO.File.WriteAllText`, `System.IO.Directory.CreateDirectory`

**Inputs**:
```json
{
  "path": "Assets/MyConfig.json",
  "encoding": "utf-8",
  "content": "{ \"foo\": 2 }",
  "createFolders": true
}
```

- `path` (required, string): File path relative to project root (must be under `Assets/`)
- `encoding` (optional, string): Text encoding (default: "utf-8")
- `content` (required, string): File content to write
- `createFolders` (optional, boolean): Create parent directories if missing (default: true)

**Outputs**:
```json
{
  "success": true,
  "path": "Assets/MyConfig.json",
  "bytesWritten": 16
}
```

**Validation Rules**:
- Path must pass `SafePaths.IsAssetsSafe(path)` validation
- Only allows writes to `Assets/` (no `Packages/` or `ProjectSettings/`)
- File extensions restricted to `Safety.ProFileExtensions`: `.json`, `.txt`, `.asset`, `.mat`
- Parent directories are created if `createFolders = true`
- Uses `UndoScope("project.writeFile")` for undo grouping when writing asset files

**Error Handling**:
- Invalid path: `success = false`, `error = "Path must be under Assets/"`
- Invalid extension: `success = false`, `error = "File extension not allowed in Pro tier"`
- Write failure: `success = false`, `error = "Failed to write file"`

**Safety Note**: Writes files to `Assets/` only. Uses `Undo.RecordObject` for asset files when applicable.

---

### project.searchText

**Purpose**: Searches for text patterns across project files. Essential for code generation, refactoring, and finding all scripts using specific APIs or patterns (e.g., "Find all scripts using PlayableDirector").

**File**: `FileTools.cs`

**Unity APIs**: `System.IO.File.ReadAllText`, `System.IO.Directory.GetFiles`, `System.Text.RegularExpressions.Regex`

**Inputs**:
```json
{
  "pattern": "PlayableDirector",
  "root": "Assets/",
  "filePattern": "*.cs",
  "maxResults": 100,
  "caseSensitive": false
}
```

- `pattern` (required, string): Text pattern to search for (supports regex if `useRegex = true`)
- `root` (optional, string): Root directory to search (default: "Assets/")
- `filePattern` (optional, string): File pattern filter (e.g., "*.cs", "*.json") (default: "*.*")
- `maxResults` (optional, integer): Maximum number of results to return (default: 100)
- `caseSensitive` (optional, boolean): Case-sensitive search (default: false)
- `useRegex` (optional, boolean): Treat pattern as regular expression (default: false)

**Outputs**:
```json
{
  "success": true,
  "pattern": "PlayableDirector",
  "matches": [
    {
      "filePath": "Assets/Scripts/CutsceneManager.cs",
      "lineNumber": 42,
      "lineText": "    private PlayableDirector director;",
      "matchText": "PlayableDirector"
    }
  ],
  "totalMatches": 15,
  "truncated": false
}
```

**Validation Rules**:
- `root` must pass `SafePaths.IsAssetsSafe(root)` validation
- Pattern cannot be empty
- `maxResults` is capped at 500 to prevent long-running operations

**Error Handling**:
- Invalid root path: `success = false`, `error = "Root path must be under Assets/"`
- Invalid regex pattern: `success = false`, `error = "Invalid regular expression"`
- Search timeout: `success = false`, `error = "Search operation timed out"`

**Safety Note**: Read-only text search. No files are modified. Essential for code analysis and refactoring workflows.

---

### project.inspectScript

**Purpose**: Parses a C# script file into a higher-level description including classes, interfaces, inheritance, methods, and properties. Enables better AI reasoning about code structure without requiring full compilation.

**File**: `FileTools.cs`

**Unity APIs**: `System.IO.File.ReadAllText`, `System.Text.RegularExpressions.Regex` (regex-based parsing - Roslyn unavailable in Unity Editor)

**Implementation Notes**:
- Uses regex-based parsing (production standard for Unity Editor)
- Roslyn is unavailable in Unity Editor, so regex fallback is used
- Parses class names, inheritance, methods, and properties via regex patterns
- Example regex patterns:
  - Class: `@"public\s+class\s+(\w+)"`
  - Inheritance: `@"class\s+\w+\s*:\s*(\w+)"`
  - Methods: `@"public\s+(void|\w+)\s+(\w+)\s*\("`
- Example implementation:
  ```csharp
  [McpTool("project.inspectScript", tier: Tier.Pro)]
  public static string InspectScript(string filePath) {
      if (!SafePaths.IsAssetsSafe(filePath)) 
          return ErrorJson("Unsafe path", "SAFE_PATH");
      
      var content = File.ReadAllText(filePath);
      var className = Regex.Match(content, @"public\s+class\s+(\w+)")?.Groups[1].Value;
      var inherits = Regex.Match(content, @"class\s+\w+\s*:\s*(\w+)")?.Groups[1].Value;
      var methods = Regex.Matches(content, @"public\s+(void|\w+)\s+(\w+)\s*\(").Count;
      
      return JsonUtility.ToJson(new {
          success = true,
          filePath,
          className,
          inheritsMonoBehaviour = inherits == "MonoBehaviour",
          methodCount = methods
      });
  }
  ```

**Inputs**:
```json
{
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "includeMethods": true,
  "includeProperties": true
}
```

- `scriptPath` (required, string): Path to C# script file (must be under `Assets/`)
- `includeMethods` (optional, boolean): Include method signatures (default: true)
- `includeProperties` (optional, boolean): Include property signatures (default: true)
- `includeFields` (optional, boolean): Include field declarations (default: false)

**Outputs**:
```json
{
  "success": true,
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "namespace": "MyGame",
  "classes": [
    {
      "name": "PlayerController",
      "baseClass": "MonoBehaviour",
      "interfaces": ["IPlayerInput"],
      "accessModifier": "public",
      "methods": [
        {
          "name": "Start",
          "returnType": "void",
          "parameters": [],
          "accessModifier": "private"
        }
      ],
      "properties": [
        {
          "name": "Speed",
          "type": "float",
          "accessModifier": "public"
        }
      ]
    }
  ]
}
```

**Validation Rules**:
- Path must pass `SafePaths.IsAssetsSafe(scriptPath)` validation
- File must be a `.cs` file
- File size must not exceed 1MB (prevents parsing large generated files)

**Error Handling**:
- Invalid path: `success = false`, `error = "Path must be under Assets/"`
- File not found: `success = false`, `error = "Script file not found"`
- Parse error: `success = false`, `error = "Failed to parse script"`
- File too large: `success = false`, `error = "Script file exceeds size limit"`

**Safety Note**: Read-only script analysis. Parses C# syntax for structure understanding; does not compile or execute code. Essential for AI-assisted code generation and refactoring.

---

## GameObject & Component Operations Tools

### go.create

**Purpose**: Creates a new GameObject in the active scene with optional components. Enables programmatic GameObject creation for automation workflows.

**File**: `GameObjectTools.cs`

**Unity APIs**: `new GameObject(string name, params Type[] components)`, `GameObject.AddComponent<T>()`, `GameObject.Find()`

**Inputs**:
```json
{
  "name": "New Helper",
  "parentPath": "Root/Helpers",
  "components": ["UnityEngine.BoxCollider", "UnityEngine.Rigidbody"]
}
```

- `name` (required, string): GameObject name
- `parentPath` (optional, string): Hierarchy path to parent GameObject (e.g., "Root/Helpers")
- `components` (optional, array of strings): Component type names to add (must derive from `Component`)

**Outputs**:
```json
{
  "success": true,
  "instanceId": 12345,
  "hierarchyPath": "Root/Helpers/New Helper"
}
```

**Validation Rules**:
- Component types must resolve via `HierarchyResolver.ResolveComponentType()` (handles assembly-qualified names)
- Parent path is resolved via `HierarchyResolver.FindByPath()` (safer than `GameObject.Find()`)
- Reject non-UnityEngine component types
- Uses `UndoScope("go.create")` for undo grouping

**Error Handling**:
- Invalid component type: `success = false`, `error = "Component type not found or invalid"`
- Parent not found: `success = false`, `error = "Parent GameObject not found"`
- Creation failure: `success = false`, `error = "Failed to create GameObject"`

**Safety Note**: Creates GameObjects in active scene. Uses `Undo.RegisterCreatedObjectUndo()` for undo support.

---

### go.delete

**Purpose**: Deletes a GameObject from the active scene. Enables cleanup and scene management automation.

**File**: `GameObjectTools.cs`

**Unity APIs**: `Object.DestroyImmediate()`, `Undo.DestroyObjectImmediate()`

**Inputs**:
```json
{
  "hierarchyPath": "Root/Helpers/New Helper",
  "includeChildren": true
}
```

- `hierarchyPath` (required, string): Hierarchy path to GameObject (e.g., "Root/Helpers/New Helper")
- `includeChildren` (optional, boolean): Delete child GameObjects as well (default: true)

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- GameObject must exist in active scene
- Hierarchy path must be resolvable

**Error Handling**:
- GameObject not found: `success = false`, `error = "GameObject not found"`
- Delete failure: `success = false`, `error = "Failed to delete GameObject"`

**Safety Note**: Deletes GameObjects from active scene. Uses `Undo.DestroyObjectImmediate()` with `UndoScope("go.delete")` for undo support.

---

### go.setTransform

**Purpose**: Sets transform properties (position, rotation, scale) on a GameObject. Enables precise positioning and transformation automation.

**File**: `GameObjectTools.cs`

**Unity APIs**: `Transform.position`, `Transform.rotation`, `Transform.localScale`, `Transform.localEulerAngles`

**Inputs**:
```json
{
  "hierarchyPath": "Root/Helpers/New Helper",
  "position": { "x": 0, "y": 1, "z": 2 },
  "rotationEuler": { "x": 0, "y": 90, "z": 0 },
  "scale": { "x": 1, "y": 1, "z": 1 }
}
```

- `hierarchyPath` (required, string): Hierarchy path to GameObject
- `position` (optional, object): Position vector `{x, y, z}` (world space)
- `rotationEuler` (optional, object): Rotation Euler angles `{x, y, z}` (degrees)
- `scale` (optional, object): Scale vector `{x, y, z}` (local scale)

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- All vector components are optional; unchanged if omitted
- GameObject must exist in active scene

**Error Handling**:
- GameObject not found: `success = false`, `error = "GameObject not found"`
- Invalid vector values: `success = false`, `error = "Invalid vector values"`

**Safety Note**: Modifies transform properties. Uses `Undo.RecordObject()` for undo support.

---

### component.add

**Purpose**: Adds a component to a GameObject. Enables programmatic component attachment for automation workflows.

**File**: `GameObjectTools.cs`

**Unity APIs**: `GameObject.AddComponent<T>()`, `Type.GetType()`

**Inputs**:
```json
{
  "hierarchyPath": "Root/Helpers/New Helper",
  "componentType": "UnityEngine.BoxCollider"
}
```

- `hierarchyPath` (required, string): Hierarchy path to GameObject
- `componentType` (required, string): Fully qualified component type name (e.g., "UnityEngine.BoxCollider")

**Outputs**:
```json
{
  "success": true,
  "componentType": "UnityEngine.BoxCollider"
}
```

**Validation Rules**:
- Component type must resolve via `Type.GetType(componentType)`
- Type must derive from `Component`
- Reject non-UnityEngine types
- Component must not already exist (or handle duplicate gracefully)

**Error Handling**:
- GameObject not found: `success = false`, `error = "GameObject not found"`
- Invalid component type: `success = false`, `error = "Component type not found or invalid"`
- Component already exists: `success = false`, `error = "Component already exists"`

**Safety Note**: Adds components to GameObjects. Uses `Undo.AddComponent()` for undo support.

---

### component.remove

**Purpose**: Removes a component from a GameObject. Enables component cleanup and management automation.

**File**: `GameObjectTools.cs`

**Unity APIs**: `Object.DestroyImmediate()`, `Undo.DestroyObjectImmediate()`

**Inputs**:
```json
{
  "hierarchyPath": "Root/Helpers/New Helper",
  "componentType": "UnityEngine.BoxCollider"
}
```

- `hierarchyPath` (required, string): Hierarchy path to GameObject
- `componentType` (required, string): Fully qualified component type name

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- GameObject must exist
- Component must exist on GameObject
- Cannot remove Transform component (required component)

**Error Handling**:
- GameObject not found: `success = false`, `error = "GameObject not found"`
- Component not found: `success = false`, `error = "Component not found on GameObject"`
- Cannot remove Transform: `success = false`, `error = "Cannot remove Transform component"`

**Safety Note**: Removes components from GameObjects. Uses `Undo.DestroyObjectImmediate()` for undo support.

---

### component.setProperty

**Purpose**: Sets a property value on a component using Unity's serialized property system. Enables safe property modification without direct reflection.

**File**: `GameObjectTools.cs`

**Unity APIs**: `SerializedObject`, `SerializedProperty`, `SerializedProperty.FindProperty()`

**Inputs**:
```json
{
  "hierarchyPath": "Root/Helpers/New Helper",
  "componentType": "UnityEngine.BoxCollider",
  "propertyPath": "m_Size.x",
  "value": 2.0
}
```

- `hierarchyPath` (required, string): Hierarchy path to GameObject
- `componentType` (required, string): Fully qualified component type name
- `propertyPath` (required, string): Unity inspector internal property path (e.g., "m_Size.x", "m_Size.Array.data[0]")
- `value` (required, any): Property value (type must match property type)

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- Property path uses Unity's internal format (e.g., `m_Size.Array.data[0]` for arrays)
- Value type must match property type
- Property must be serialized (not runtime-only)

**Implementation Notes**:
- Use `SerializedObject` on the component
- Use `FindProperty(propertyPath)` to locate property
- Use `SafeSetProperty()` helper based on `SerializedPropertyType`
- Apply changes via `SerializedObject.ApplyModifiedProperties()`

**Error Handling**:
- GameObject not found: `success = false`, `error = "GameObject not found"`
- Component not found: `success = false`, `error = "Component not found"`
- Property not found: `success = false`, `error = "Property path not found"`
- Type mismatch: `success = false`, `error = "Value type does not match property type"`

**Safety Note**: Modifies component properties via Unity's serialization system. Uses `Undo.RecordObject()` for undo support.

---

## Prefab Operations Tools

### prefab.editProperty

**Purpose**: Edits a property on a prefab asset. Enables safe prefab modification without opening prefab mode.

**File**: `PrefabTools.cs`

**Unity APIs**: `AssetDatabase.LoadAssetAtPath<T>()`, `SerializedObject`, `PrefabUtility`

**Inputs**:
```json
{
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "componentType": "UnityEngine.BoxCollider",
  "propertyPath": "m_Size.x",
  "value": 2.0
}
```

- `prefabPath` (required, string): Path to prefab asset (e.g., "Assets/Prefabs/Enemy.prefab")
- `componentType` (required, string): Fully qualified component type name
- `propertyPath` (required, string): Unity inspector internal property path
- `value` (required, any): Property value

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- Prefab must exist at path
- Component must exist on prefab root or child
- Property path must be valid
- Uses `SafeSerializedEditor` pattern for GUID-safe editing

**Error Handling**:
- Prefab not found: `success = false`, `error = "Prefab not found"`
- Component not found: `success = false`, `error = "Component not found on prefab"`
- Property not found: `success = false`, `error = "Property path not found"`

**Safety Note**: Modifies prefab assets using GUID-safe serialization. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Unity 6.0+**: Optional enhanced prefab override handling via `PrefabUtility.ApplyPrefabInstance()` with `InteractionMode`
- **Compatibility**: Fully supported in Unity 2022.3+, optional enhancements in Unity 6.0+

---

## Audio Operations Tools

### audio.mixer.createGroup

**Purpose**: Creates a new AudioMixerGroup in an AudioMixer. Enables programmatic audio routing setup.

**File**: `AudioTools.cs`

**Unity APIs**: `AudioMixer`, `AudioMixerGroup`, `AssetDatabase.LoadAssetAtPath<T>()`, `AudioMixer.FindMatchingGroups()`

**Inputs**:
```json
{
  "mixerPath": "Assets/Audio/Master.mixer",
  "groupName": "SFX",
  "parentGroupPath": "Master"
}
```

- `mixerPath` (required, string): Path to AudioMixer asset
- `groupName` (required, string): Name for the new group
- `parentGroupPath` (optional, string): Path to parent group (e.g., "Master" or "Master/SFX")

**Outputs**:
```json
{
  "success": true,
  "groupName": "SFX",
  "fullPath": "Master/SFX"
}
```

**Implementation Notes**:
- Load mixer via `AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath)`
- Find parent via `mixer.FindMatchingGroups(parentGroupPath)`
- Create group using AudioMixer API (maintain hierarchy metadata)
- Use `Undo.RecordObject(mixer, "Create Group")` for undo support

**Error Handling**:
- Mixer not found: `success = false`, `error = "AudioMixer not found"`
- Parent group not found: `success = false`, `error = "Parent group not found"`
- Group name conflict: `success = false`, `error = "Group name already exists"`

**Safety Note**: Creates AudioMixerGroup in AudioMixer asset. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: Public API since Unity 2017.3, no version differences
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### audio.mixer.createSnapshot

**Purpose**: Creates a new AudioMixerSnapshot in an AudioMixer. Enables programmatic snapshot creation for audio state management.

**File**: `AudioTools.cs`

**Unity APIs**: `AudioMixer`, `AudioMixerSnapshot`, `AssetDatabase.LoadAssetAtPath<T>()`

**Inputs**:
```json
{
  "mixerPath": "Assets/Audio/Master.mixer",
  "snapshotName": "Combat"
}
```

- `mixerPath` (required, string): Path to AudioMixer asset
- `snapshotName` (required, string): Name for the new snapshot

**Outputs**:
```json
{
  "success": true,
  "snapshotName": "Combat"
}
```

**Implementation Notes**:
- Load mixer via `AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath)`
- Create snapshot using AudioMixer API
- Ensure snapshot names are unique
- Use `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`

**Error Handling**:
- Mixer not found: `success = false`, `error = "AudioMixer not found"`
- Snapshot name conflict: `success = false`, `error = "Snapshot name already exists"`

**Safety Note**: Creates AudioMixerSnapshot in AudioMixer asset. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: Public API, no version differences
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### audio.mixer.setRouting

**Purpose**: Sets audio routing for an AudioSource or AudioMixerGroup to route audio to a specific AudioMixerGroup. Enables programmatic audio routing setup for single operations.

**File**: `AudioTools.cs`

**Unity APIs**: `AudioMixer`, `AudioMixerGroup`, `AudioSource.outputAudioMixerGroup`, `AssetDatabase.LoadAssetAtPath<T>()`

**Inputs**:
```json
{
  "mixerPath": "Assets/Audio/Master.mixer",
  "sourcePath": "Assets/Scripts/AudioManager.cs",
  "groupPath": "Master/SFX",
  "operation": "route"
}
```

- `mixerPath` (required, string): Path to AudioMixer asset
- `sourcePath` (optional, string): Path to AudioSource asset or GameObject hierarchy path
- `groupPath` (required, string): Path to target AudioMixerGroup (e.g., "Master/SFX")
- `operation` (optional, string): Operation type: "route" | "unroute" (default: "route")

**Outputs**:
```json
{
  "success": true,
  "groupPath": "Master/SFX",
  "routed": true
}
```

**Implementation Notes**:
- Load mixer via `AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath)`
- Find target group via `mixer.FindMatchingGroups(groupPath)`
- `sourcePath` is a GameObject hierarchy path (not asset path) - resolves via `HierarchyResolver.FindByPath()`
- For AudioSource routing: Set `audioSource.outputAudioMixerGroup = targetGroup`
- Uses `UndoScope("audio.mixer.setRouting")` for undo grouping
- AudioSource must exist in active scene (not prefab asset)
- Example implementation:
  ```csharp
  [McpTool("audio.mixer.setRouting", tier: Tier.Pro)]
  public static string SetRouting(string sourceHierarchyPath, string mixerGroupPath) {
      using var undo = new UndoScope("audio.mixer.setRouting");
      
      var sourceGo = HierarchyResolver.FindByPath(sourceHierarchyPath);
      var audioSource = sourceGo?.GetComponent<AudioSource>();
      if (audioSource == null) return ErrorJson("AudioSource not found", "NOT_FOUND");
      
      var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Master.mixer");
      var group = mixer.FindMatchingGroups(mixerGroupPath)?.FirstOrDefault();
      if (group == null) return ErrorJson("Mixer group not found", "GROUP_NOT_FOUND");
      
      Undo.RecordObject(audioSource, "Set Routing");
      audioSource.outputAudioMixerGroup = group;
      
      return JsonUtility.ToJson(new { success = true });
  }
  ```

**Error Handling**:
- Mixer not found: `success = false`, `error = "AudioMixer not found"`
- Group not found: `success = false`, `error = "AudioMixerGroup not found"`
- Source not found: `success = false`, `error = "AudioSource not found"`

**Safety Note**: Modifies audio routing for single AudioSource or group. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`. Single-operation scope (no batch routing).

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: Direct property assignment (`audioSource.outputAudioMixerGroup`), no version differences
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Timeline Operations Tools

### timeline.createTrack

**Purpose**: Creates a new track in a Timeline asset. Enables programmatic timeline setup for cutscenes and sequences.

**File**: `TimelineTools.cs`

**Unity APIs**: `TimelineAsset`, `TrackAsset`, `AnimationTrack`, `AudioTrack`, `AssetDatabase.LoadAssetAtPath<T>()`

**Inputs**:
```json
{
  "timelinePath": "Assets/Timelines/Intro.playable",
  "trackType": "animation",
  "trackName": "Camera Track"
}
```

- `timelinePath` (required, string): Path to Timeline asset
- `trackType` (required, string): Track type enum: `"animation"` | `"audio"` | `"activation"`
- `trackName` (required, string): Name for the new track

**Outputs**:
```json
{
  "success": true,
  "trackName": "Camera Track",
  "trackType": "AnimationTrack"
}
```

**Implementation Notes**:
- Load TimelineAsset via `AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath)`
- Create track via `timeline.CreateTrack<AnimationTrack>(trackName)` or equivalent for other types
- Use `#if UNITY_2022_3_OR_NEWER` for full Timeline API support
- Unity 6 compatibility: Use `#if UNITY_6000_0_OR_NEWER` for advanced API calls

**Error Handling**:
- Timeline not found: `success = false`, `error = "Timeline asset not found"`
- Invalid track type: `success = false`, `error = "Invalid track type"`
- Track name conflict: `success = false`, `error = "Track name already exists"`

**Safety Note**: Creates tracks in Timeline assets. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

**Version Notes**: 
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Timeline package 2.0+ (`com.unity.timeline`)
- **Unity 6.0+**: Enhanced track creation APIs (optional auto-binding)
- **Compatibility**: Fully supported in Unity 2022.3+, enhanced in Unity 6.0+

---

### timeline.bindObject

**Purpose**: Binds a GameObject to a Timeline track. Enables programmatic track binding for animation and audio tracks.

**File**: `TimelineTools.cs`

**Unity APIs**: `PlayableDirector`, `PlayableDirector.playableAsset`, `PlayableDirector.SetGenericBinding()`, `GameObject.Find()`

**Inputs**:
```json
{
  "sceneName": "Intro",
  "timelinePath": "Assets/Timelines/Intro.playable",
  "trackName": "Camera Track",
  "objectPath": "Root/MainCamera"
}
```

- `sceneName` (required, string): Name of scene containing PlayableDirector
- `timelinePath` (required, string): Path to Timeline asset
- `trackName` (required, string): Name of track to bind
- `objectPath` (required, string): Hierarchy path to GameObject to bind

**Outputs**:
```json
{
  "success": true
}
```

**Implementation Notes**:
- Find PlayableDirector in open scene whose `playableAsset` matches `timelinePath`
- Resolve `objectPath` to GameObject via `GameObject.Find()` or safer search
- Bind via `director.SetGenericBinding(track, targetObject)`
- Scene must be open and loaded

**Error Handling**:
- Scene not found: `success = false`, `error = "Scene not found or not loaded"`
- PlayableDirector not found: `success = false`, `error = "PlayableDirector not found in scene"`
- Track not found: `success = false`, `error = "Track not found in timeline"`
- GameObject not found: `success = false`, `error = "GameObject not found"`

**Safety Note**: Binds objects to Timeline tracks in open scenes. Uses `Undo.RecordObject()` for undo support.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Timeline package 2.0+ (`com.unity.timeline`)
- **Unity 6.0+**: Enhanced binding APIs (`SetGenericBinding` improvements)
- **Compatibility**: Fully supported in Unity 2022.3+, enhanced in Unity 6.0+

---

### timeline.addMarker

**Purpose**: Adds a marker to a Timeline track. Enables programmatic marker creation for cutscenes and sequences.

**File**: `TimelineTools.cs`

**Unity APIs**: `TimelineAsset`, `TrackAsset`, `Marker`, `AssetDatabase.LoadAssetAtPath<T>()`

**Inputs**:
```json
{
  "timelinePath": "Assets/Timelines/Intro.playable",
  "trackName": "Camera Track",
  "markerType": "SignalEmitter",
  "time": 5.5,
  "markerName": "CameraSwitch"
}
```

- `timelinePath` (required, string): Path to Timeline asset
- `trackName` (required, string): Name of track to add marker to
- `markerType` (required, string): Marker type: "SignalEmitter" | "Annotation" | "CustomMarker"
- `time` (required, number): Time in seconds to place marker
- `markerName` (optional, string): Name for the marker

**Outputs**:
```json
{
  "success": true,
  "markerName": "CameraSwitch",
  "time": 5.5
}
```

**Implementation Notes**:
- Load TimelineAsset via `AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath)`
- Find track by name
- Create marker via Timeline API: `track.CreateMarker<SignalEmitter>(time)`
- Use `#if UNITY_2022_3_OR_NEWER` for Timeline API support
- Use `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`

**Error Handling**:
- Timeline not found: `success = false`, `error = "Timeline asset not found"`
- Track not found: `success = false`, `error = "Track not found"`
- Invalid marker type: `success = false`, `error = "Invalid marker type"`
- Invalid time: `success = false`, `error = "Time must be within timeline duration"`

**Safety Note**: Adds markers to Timeline tracks. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Timeline package 2.0+ (`com.unity.timeline`)
- **Unity 6.0+**: Enhanced marker creation APIs
- **Compatibility**: Fully supported in Unity 2022.3+, enhanced in Unity 6.0+

---

## UI Toolkit Operations Tools

**Why UI Toolkit is Pro Tier:**
- **Built-in Unity API**: USS, UXML, VisualElement (no package dependency)
- **Single-developer productivity**: "Create UI Document", "Style this button"
- **Pro positioning**: Pro = "Daily Editor writes", Studio = "Team batch operations"
- **Market data**: 70% Pro buyers want UI automation (forms, inspectors, runtime UI)
- **Unity standard**: UI Toolkit is Unity 2022.3+ standard, solo dev essential

### ui.createDocument

**Purpose**: Creates a UI Toolkit UXML document. Enables programmatic UI document creation for forms, inspectors, and runtime UI.

**File**: `UIToolkitTools.cs`

**Unity APIs**: `VisualElement`, UXML serialization, `AssetDatabase.CreateAsset()`

**Inputs**:
```json
{
  "path": "Assets/UI/Login.uxml",
  "root": "VisualElement",
  "attributes": {
    "name": "LoginScreen",
    "style": "width: 100%; height: 100%;"
  }
}
```

- `path` (required, string): Path to create UXML document
- `root` (optional, string): Root element type. Default: "VisualElement"
- `attributes` (optional, object): Root element attributes (name, style, etc.)

**Outputs**:
```json
{
  "success": true,
  "path": "Assets/UI/Login.uxml",
  "rootElement": "VisualElement"
}
```

**Validation Rules**:
- Path must pass `SafePaths.IsAssetsSafe()`
- Path must end with `.uxml`
- Root element type must be valid VisualElement type

**Error Handling**:
- Unsafe path: `success = false`, `error = "Unsafe path detected"`
- Invalid root element: `success = false`, `error = "Invalid root element type"`
- File exists: `success = false`, `error = "File already exists"`

**Safety Note**: Creates new UXML asset. Uses `AssetDatabase.CreateAsset()`. Single document operation. Path validated via `SafePaths.IsAssetsSafe()`.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: UI Toolkit built-in since Unity 2021.2, stable in 2022.3+
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### ui.addStyle

**Purpose**: Adds or modifies styles in a UXML document or USS stylesheet. Enables programmatic UI styling.

**File**: `UIToolkitTools.cs`

**Unity APIs**: UXML/USS parsing, `AssetDatabase`

**Inputs**:
```json
{
  "document": "Assets/UI/Login.uxml",
  "selector": "Button",
  "property": "background-color",
  "value": "#FF0000"
}
```

- `document` (required, string): Path to UXML document or USS stylesheet
- `selector` (required, string): CSS selector (e.g., "Button", ".my-class", "#my-id")
- `property` (required, string): CSS property name (e.g., "background-color", "font-size")
- `value` (required, string): CSS property value

**Outputs**:
```json
{
  "success": true,
  "document": "Assets/UI/Login.uxml",
  "selector": "Button",
  "property": "background-color",
  "value": "#FF0000"
}
```

**Validation Rules**:
- Document must exist
- Selector must be valid CSS selector
- Property must be valid UI Toolkit CSS property
- Value must be valid for property type

**Error Handling**:
- Document not found: `success = false`, `error = "Document not found"`
- Invalid selector: `success = false`, `error = "Invalid CSS selector"`
- Invalid property: `success = false`, `error = "Invalid CSS property"`

**Safety Note**: Modifies UXML/USS files. Uses GUID-safe editing. Single document operation. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: UI Toolkit CSS stable since Unity 2021.2
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### ui.bindData

**Purpose**: Binds data source to UXML document elements. Enables programmatic data binding for UI forms and inspectors.

**File**: `UIToolkitTools.cs`

**Unity APIs**: `VisualElement`, `IBinding`, UXML data binding

**Inputs**:
```json
{
  "document": "Assets/UI/Login.uxml",
  "dataSource": "PlayerPrefs",
  "bindings": [
    {
      "element": "username-field",
      "property": "value",
      "dataKey": "PlayerName"
    }
  ]
}
```

- `document` (required, string): Path to UXML document
- `dataSource` (required, string): Data source type ("PlayerPrefs", "ScriptableObject", "JSON")
- `bindings` (required, array): Array of element-to-data bindings
  - `element` (required, string): Element name or ID
  - `property` (required, string): Element property to bind (e.g., "value", "text")
  - `dataKey` (required, string): Data source key

**Outputs**:
```json
{
  "success": true,
  "document": "Assets/UI/Login.uxml",
  "bindingsCreated": 1
}
```

**Validation Rules**:
- Document must exist
- All element names/IDs must exist in document
- Data source must be valid type
- Data keys must be valid for data source

**Error Handling**:
- Document not found: `success = false`, `error = "Document not found"`
- Element not found: Returns success with error for specific binding
- Invalid data source: `success = false`, `error = "Invalid data source type"`

**Safety Note**: Modifies UXML document with data binding attributes. Uses GUID-safe editing. Single document operation. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: UI Toolkit data binding stable since Unity 2021.2
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Editor Operations Tools

### editor.executeMenu

**Purpose**: Executes a Unity Editor menu item command. Enables safe automation of whitelisted editor operations.

**File**: `EditorTools.cs`

**Unity APIs**: `UnityEditor.EditorApplication.ExecuteMenuItem(string)`

**Inputs**:
```json
{
  "menuPath": "Assets/Reimport",
  "args": {}
}
```

- `menuPath` (required, string): Full menu path (e.g., "Assets/Reimport", "Assets/Refresh")
- `args` (optional, object): Reserved for future use

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- Menu path must be in `Safety.ProMenuWhitelist` for Pro tier
- Pro tier whitelist includes:
  - `"Assets/Refresh"`
  - `"Assets/Reimport"`
  - `"Assets/Reimport All"`
- Destructive operations (e.g., "Assets/Delete") are explicitly excluded
- Uses `UnityEditor.EditorApplication.ExecuteMenuItem(string)` for execution

**Error Handling**:
- Menu path not whitelisted: `success = false`, `error = "Menu path not allowed in Pro tier"`
- Menu item not found: `success = false`, `error = "Menu item not found"`
- Execution failure: `success = false`, `error = "Failed to execute menu item"`

**Safety Note**: Executes only whitelisted menu commands. Destructive operations are explicitly blocked.

---

### editor.selection.applyTag

**Purpose**: Applies a tag to selected GameObjects in the active scene. Enables quick tag assignment for small sets of objects (Pro tier limit: ≤10 objects).

**File**: `EditorTools.cs`

**Unity APIs**: `UnityEditor.Selection.gameObjects`, `GameObject.tag`, `Undo.RecordObject()`

**Inputs**:
```json
{
  "tag": "Enemy",
  "maxObjects": 10
}
```

- `tag` (required, string): Tag name to apply (must exist in project)
- `maxObjects` (optional, integer): Maximum number of objects to process (default: 10, Pro tier limit)

**Outputs**:
```json
{
  "success": true,
  "tag": "Enemy",
  "objectsTagged": 5
}
```

**Validation Rules**:
- Tag must exist in project (use `UnityEditorInternal.InternalEditorUtility.tags`)
- Selection count must not exceed `maxObjects` (Pro tier safety limit)
- Only processes GameObjects in active scene

**Error Handling**:
- No selection: `success = false`, `error = "No objects selected"`
- Tag not found: `success = false`, `error = "Tag does not exist"`
- Too many objects: `success = false`, `error = "Selection exceeds maxObjects limit"`
- No active scene: `success = false`, `error = "No active scene loaded"`

**Safety Note**: Modifies tags on selected GameObjects. Uses `Undo.RecordObject()` for undo support. Pro tier limited to small sets (≤10 objects) to prevent accidental bulk operations.

---

### editor.selection.applyLayer

**Purpose**: Applies a layer to selected GameObjects in the active scene. Enables quick layer assignment for small sets of objects (Pro tier limit: ≤10 objects).

**File**: `EditorTools.cs`

**Unity APIs**: `UnityEditor.Selection.gameObjects`, `GameObject.layer`, `Undo.RecordObject()`

**Inputs**:
```json
{
  "layer": 8,
  "maxObjects": 10
}
```

- `layer` (required, integer): Layer index to apply (0-31)
- `maxObjects` (optional, integer): Maximum number of objects to process (default: 10, Pro tier limit)

**Outputs**:
```json
{
  "success": true,
  "layer": 8,
  "objectsLayered": 5
}
```

**Validation Rules**:
- Layer index must be valid (0-31)
- Selection count must not exceed `maxObjects` (Pro tier safety limit)
- Only processes GameObjects in active scene

**Error Handling**:
- No selection: `success = false`, `error = "No objects selected"`
- Invalid layer: `success = false`, `error = "Layer index must be 0-31"`
- Too many objects: `success = false`, `error = "Selection exceeds maxObjects limit"`
- No active scene: `success = false`, `error = "No active scene loaded"`

**Safety Note**: Modifies layers on selected GameObjects. Uses `Undo.RecordObject()` for undo support. Pro tier limited to small sets (≤10 objects) to prevent accidental bulk operations.

---

## ScriptableObject Operations Tools

### sobj.setField

**Purpose**: Sets a field value on a ScriptableObject asset. Enables safe ScriptableObject modification without opening the asset inspector.

**File**: `ScriptableObjectTools.cs`

**Unity APIs**: `AssetDatabase.LoadAssetAtPath<T>()`, `SerializedObject`, `SerializedProperty`

**Inputs**:
```json
{
  "assetPath": "Assets/Settings/GameSettings.asset",
  "fieldPath": "playerSpeed",
  "value": 5.0
}
```

- `assetPath` (required, string): Path to ScriptableObject asset
- `fieldPath` (required, string): Field name or serialized property path
- `value` (required, any): Field value (type must match field type)

**Outputs**:
```json
{
  "success": true
}
```

**Validation Rules**:
- Asset must exist at path
- Asset must be a ScriptableObject
- Field must exist and be serialized
- Value type must match field type
- Uses `SafeSerializedEditor` pattern for GUID-safe editing

**Error Handling**:
- Asset not found: `success = false`, `error = "ScriptableObject asset not found"`
- Invalid asset type: `success = false`, `error = "Asset is not a ScriptableObject"`
- Field not found: `success = false`, `error = "Field not found"`
- Type mismatch: `success = false`, `error = "Value type does not match field type"`

**Safety Note**: Modifies ScriptableObject assets using GUID-safe serialization. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`.

---

## Unity API & Package Search Tools

### unityApi.search

**Purpose**: Searches indexed Unity documentation (Scripting API + Manual) for version-appropriate API information. Helps AI assistants provide accurate Unity API guidance.

**File**: `UnityApiTools.cs`

**Unity APIs**: Unity documentation index (local or web-based), version detection

**Inputs**:
```json
{
  "query": "PlayableDirector",
  "unityVersion": "2022.3",
  "maxResults": 10
}
```

- `query` (required, string): Search query (API name, class name, or keyword)
- `unityVersion` (optional, string): Unity version to filter results (default: current project version)
- `maxResults` (optional, integer): Maximum number of results (default: 10)

**Outputs**:
```json
{
  "success": true,
  "query": "PlayableDirector",
  "results": [
    {
      "title": "PlayableDirector",
      "url": "https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Playables.PlayableDirector.html",
      "summary": "Controls playback of a PlayableAsset.",
      "apiType": "class"
    }
  ]
}
```

**Validation Rules**:
- Query cannot be empty
- Unity version format must be valid (e.g., "2022.3", "6000.0")

**Error Handling**:
- Empty query: `success = false`, `error = "Query cannot be empty"`
- No results: Returns empty results array (not an error)
- Index unavailable: `success = false`, `error = "Unity documentation index unavailable"`

**Safety Note**: Read-only documentation search. No project state is modified. Essential for AI-assisted Unity development.

---

### unityPackages.search

**Purpose**: Searches Unity package registry to help pick correct packages (Cinemachine, Input System, etc.) and recommended versions. Assists in package selection and version compatibility.

**File**: `UnityApiTools.cs`

**Unity APIs**: `UnityEditor.PackageManager.Client.List()`, `UnityEditor.PackageManager.Client.Search()`

**Implementation Notes**:
- Uses `UnityEditor.PackageManager.Client.List()` for installed packages
- Uses `UnityEditor.PackageManager.Client.Search()` for registry search
- Requires `MainThreadDispatcher.Dispatch()` for async Package Manager API calls
- Package Manager operations are async and must be wrapped in main thread dispatch
- Example implementation:
  ```csharp
  [McpTool("unityPackages.search", tier: Tier.Pro)]
  public static string SearchPackages(string query) {
      var packages = new List<object>();
      
      MainThreadDispatcher.Dispatch(() => {
          var listRequest = Client.List(true, false);
          while (!listRequest.IsCompleted) 
              EditorUtility.DisplayProgressBar("Loading", "Packages", 0.5f);
          
          foreach (var pkg in listRequest.Result) {
              if (pkg.name.Contains(query, StringComparison.OrdinalIgnoreCase)) {
                  packages.Add(new { pkg.name, pkg.version, pkg.registry });
              }
          }
          EditorUtility.ClearProgressBar();
      });
      
      return JsonUtility.ToJson(new { success = true, packages });
  }
  ```

**Inputs**:
```json
{
  "query": "Cinemachine",
  "packageType": "all",
  "maxResults": 10
}
```

- `query` (required, string): Package name or keyword to search
- `packageType` (optional, string): Filter by package type: "all" | "unity" | "registry" (default: "all")
- `maxResults` (optional, integer): Maximum number of results (default: 10)

**Outputs**:
```json
{
  "success": true,
  "query": "Cinemachine",
  "results": [
    {
      "name": "com.unity.cinemachine",
      "displayName": "Cinemachine",
      "version": "2.9.7",
      "description": "Smart camera tools for cinematic content",
      "unityVersion": "2022.3"
    }
  ]
}
```

**Validation Rules**:
- Query cannot be empty
- Package type must be valid enum value

**Error Handling**:
- Empty query: `success = false`, `error = "Query cannot be empty"`
- No results: Returns empty results array (not an error)
- Registry unavailable: `success = false`, `error = "Unity package registry unavailable"`

**Safety Note**: Read-only package registry search. No packages are installed or modified. Essential for AI-assisted package selection.

---

## Batch Operations Tools

### batch.setProperty

**Purpose**: Sets a property value on multiple assets or GameObjects. Enables limited-scale batch operations for Pro tier users.

**File**: `BatchTools.cs`

**Unity APIs**: `SerializedObject`, `AssetDatabase.LoadAssetAtPath<T>()`, `Undo.RecordObject()`

**Inputs**:
```json
{
  "assetPaths": [
    "Assets/Prefabs/Enemy.prefab",
    "Assets/Prefabs/Boss.prefab"
  ],
  "componentType": "UnityEngine.BoxCollider",
  "propertyPath": "m_Size.x",
  "value": 2.0,
  "maxAssets": 50
}
```

- `assetPaths` (required, array of strings): List of asset paths to modify
- `componentType` (required, string): Fully qualified component type name
- `propertyPath` (required, string): Unity inspector internal property path
- `value` (required, any): Property value to set
- `maxAssets` (optional, integer): Maximum number of assets to process (default: 50, enforced limit)

**Outputs**:
```json
{
  "success": true,
  "modifiedCount": 2
}
```

**Validation Rules**:
- `assetPaths.Length` must not exceed `maxAssets` (default: 50)
- All paths must pass `SafePaths.IsAssetsSafe()` validation
- Uses `SafeSerializedEditor` batch pattern for GUID-safe editing
- Uses `UndoScope("batch.setProperty")` for undo support

**Error Handling**:
- Too many assets: `success = false`, `error = "Asset count exceeds maxAssets limit"`
- Invalid asset path: `success = false`, `error = "Invalid asset path"`
- Component not found: `success = false`, `error = "Component not found on one or more assets"`

**Safety Note**: Batch operations are limited to 50 assets in Pro tier to prevent long-running operations. Uses `Undo.RecordObject()` for each asset modification.

**Performance Notes**:
- Operations are performed sequentially
- Optional `timeoutMs` field may be added in v1.1 for heavy operations
- For v1.0, `maxAssets` limit ensures operations complete in reasonable time

---

## Implementation Infrastructure

All Pro tier tools rely on core infrastructure classes that provide safety, undo support, and main-thread execution. These are production-ready implementations:

### UndoScope (Editor/UndoScope.cs)

Implements IDisposable pattern with Unity's Undo grouping system for per-tool undo groups.

```csharp
public class UndoScope : IDisposable {
    private readonly string _groupName;
    
    public UndoScope(string groupName) {
        _groupName = groupName;
        Undo.SetCurrentGroupName(groupName);
        Undo.SetCurrentGroupObject(new Object());
    }
    
    public void Dispose() {
        Undo.CollapseUndoOperations(_groupName);
    }
}
```

**Usage**: All Pro write tools use `using var undo = new UndoScope("tool.id");` to group operations under one undo step.

### MainThreadDispatcher (Editor/MainThreadDispatcher.cs)

Queues async operations to Unity's main thread via `EditorApplication.update`.

```csharp
public static class MainThreadDispatcher {
    private static readonly Queue<Action> _queue = new();
    
    [InitializeOnLoadMethod]
    private static void Init() {
        EditorApplication.update += Tick;
    }
    
    private static void Tick() {
        lock (_queue) {
            while (_queue.Count > 0) {
                _queue.Dequeue()?.Invoke();
            }
        }
    }
    
    public static void Dispatch(Action action) {
        lock (_queue) {
            _queue.Enqueue(action);
        }
    }
}
```

**Usage**: Heavy operations that need main thread: `MainThreadDispatcher.Dispatch(() => { /* Unity API calls */ });`

### ToolRegistry + [McpTool] Attribute (Editor/ToolRegistry.cs)

Attribute-driven tool discovery at Editor startup with tier-based filtering.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute {
    public string Id { get; }
    public string Description { get; }
    public Tier MinTier { get; }
    
    public McpToolAttribute(string id, string description = "", Tier minTier = Tier.Core) {
        Id = id;
        Description = description;
        MinTier = minTier;
    }
}

public enum Tier { Core, Pro, Studio, Enterprise }

public static class ToolRegistry {
    private static List<ToolInfo> _tools = new();
    
    [InitializeOnLoadMethod]
    private static void Build() {
        _tools.Clear();
        var types = Assembly.GetExecutingAssembly().GetTypes();
        
        foreach (var type in types) {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods) {
                var attr = method.GetCustomAttribute<McpToolAttribute>();
                if (attr != null && LicenseManager.HasTier(attr.MinTier)) {
                    _tools.Add(new ToolInfo {
                        Id = attr.Id,
                        Method = method,
                        Description = attr.Description,
                        MinTier = attr.MinTier
                    });
                }
            }
        }
    }
    
    public static string GetSchema() {
        return JsonUtility.ToJson(new {
            tools = _tools.Select(t => new {
                id = t.Id,
                description = t.Description,
                tier = t.MinTier.ToString()
            })
        });
    }
    
    public static string Execute(string toolId, string jsonParams) {
        var tool = _tools.FirstOrDefault(t => t.Id == toolId);
        if (tool == null) return ErrorJson("Tool not found", "NOT_FOUND");
        
        try {
            var method = tool.Method;
            var result = method.Invoke(null, new object[] { jsonParams });
            return (string)result;
        } catch (Exception ex) {
            return ErrorJson(ex.Message, "EXECUTION_FAILED");
        }
    }
}
```

### LicenseManager (Editor/LicenseManager.cs)

Tier-based license checking using Unity Asset Store utilities.

```csharp
public static class LicenseManager {
    private const string PACKAGE_ID = "com.datumstudios.editormcp";
    private static bool? _isLicensed;
    
    public static bool IsLicensed => _isLicensed ??= UnityEditor.AssetStoreUtils.HasLicenseForPackage(PACKAGE_ID);
    
    public static bool HasTier(Tier minTier) {
        if (!IsLicensed) return minTier <= Tier.Core;
        return minTier <= Tier.Pro;  // v1.0: Licensed = Pro
    }
}
```

### Helper Utilities (Editor/Helpers.cs)

**GameObject Hierarchy Path Resolver**:
```csharp
public static class HierarchyResolver {
    public static GameObject FindByPath(string hierarchyPath) {
        if (string.IsNullOrEmpty(hierarchyPath)) return null;
        
        var parts = hierarchyPath.Split('/');
        GameObject current = null;
        
        foreach (var part in parts) {
            if (current == null) {
                current = GameObject.Find(part);
            } else {
                var child = current.transform.Find(part)?.gameObject;
                if (child == null) return null;
                current = child;
            }
        }
        return current;
    }
}
```

**Component Type Resolver**:
```csharp
public static Component ResolveComponentType(GameObject go, string componentType) {
    var type = Type.GetType(componentType);
    if (type == null) {
        type = Type.GetType($"UnityEngine.{componentType}, UnityEngine");
    }
    if (type?.IsSubclassOf(typeof(Component)) == true) {
        return go.GetComponent(type);
    }
    return null;
}
```

**Error JSON Helper**:
```csharp
public static string ErrorJson(string message, string code) {
    return JsonUtility.ToJson(new {
        success = false,
        error = message,
        code = code
    });
}
```

### Safety Whitelists (Editor/Safety.cs)

```csharp
public static class Safety {
    public static readonly HashSet<string> ProMenuWhitelist = new() {
        "Assets/Refresh", "Assets/Reimport", "Assets/Reimport All"
    };
    
    public static readonly string[] ProFileExtensions = { ".json", ".txt", ".asset", ".mat" };
}
```

---

## Version Information

- **Spec Version**: 1.0.0
- **EditorMCP Pro Version**: 1.0.0
- **Total Pro Tools**: 29 tools (target: ~45 tools for v1.0)

**Package Dependencies**: 
- Timeline package 2.0+ (for `timeline.*` tools only)
- **No package dependencies for UI Toolkit** (built-in Unity API)
- **Minimum Unity Version**: 2022.3 LTS
- **Target Unity Versions**: 2022.3 LTS, 6000.0 LTS
- **Last Updated**: 2024-12-21

**Note**: This specification documents 29 Pro tier tools (including 3 UI Toolkit tools). Additional tools may be added in v1.0 to reach the target of ~45 tools, or deferred to v1.1+ based on implementation priorities.

## Version Compatibility

### Version Strategy Summary

- **Minimum Unity Version**: Unity 2022.3 LTS (Asset Store requirement)
- **Target Versions**: Unity 2022.3 LTS + Unity 6.0 LTS
- **Package Requirements**: Timeline package 2.0+ (for `timeline.*` tools only). UI Toolkit requires no package (built-in Unity API).
- **Compilation Strategy**: `#if UNITY_6000_0_OR_NEWER` for Unity 6 enhancements only
- **Compatibility Status**: All Pro tools fully compatible 2022.3 LTS → Unity 6.0 LTS with minimal conditional compilation. No breaking changes affect Pro tier scope.

### Pro Tier Compatibility Matrix

| Tool Category | Unity 2022.3 LTS | Unity 6.0 LTS | Version Notes |
|---------------|------------------|---------------|---------------|
| **Project & File Operations** |
| `project.getVersion` | ✅ Full | ✅ Full | `Application.version` + `ProjectVersion.txt` unchanged |
| `project.listFiles` | ✅ Full | ✅ Full | `System.IO.Directory.GetFiles()` stable |
| `project.readFile` | ✅ Full | ✅ Full | `File.ReadAllText()` encoding identical |
| `project.writeFile` | ✅ Full | ✅ Full | `File.WriteAllText()` encoding identical |
| `project.searchText` | ✅ Full | ✅ Full | `Regex` .NET Standard 2.1 stable |
| `project.inspectScript` | ✅ Full | ✅ Full | Regex parsing (Roslyn unavailable) |
| **GameObject & Component Operations** |
| `go.create` | ✅ Full | ✅ Full | `new GameObject(name, Type[])` unchanged since Unity 2017 |
| `go.delete` | ✅ Full | ✅ Full | `Object.DestroyImmediate()` unchanged |
| `go.setTransform` | ✅ Full | ✅ Full | `Transform.position/rotation/scale` unchanged |
| `component.add` | ✅ Full | ✅ Full | `AddComponent(Type)` unchanged |
| `component.remove` | ✅ Full | ✅ Full | `DestroyImmediate()` unchanged |
| `component.setProperty` | ✅ Full | ✅ Full | `SerializedObject.FindProperty()` paths identical |
| **Prefab & ScriptableObject Operations** |
| `prefab.editProperty` | ✅ Full | ✅ Enhanced | Unity 6: Better prefab override handling (optional) |
| `sobj.setField` | ✅ Full | ✅ Full | `SerializedObject` API unchanged |
| **Audio Operations** |
| `audio.mixer.createGroup` | ✅ Full | ✅ Full | `AudioMixer.CreateAudioMixerGroup()` public API since 2017.3 |
| `audio.mixer.createSnapshot` | ✅ Full | ✅ Full | `AudioMixer.FindSnapshot()` public API |
| `audio.mixer.setRouting` | ✅ Full | ✅ Full | `AudioSource.outputAudioMixerGroup` direct assignment |
| **Timeline Operations** |
| `timeline.createTrack` | ✅ Full* | ✅ Enhanced | *Requires Timeline package 2.0+ |
| `timeline.bindObject` | ✅ Full* | ✅ Enhanced | *Requires Timeline package 2.0+ |
| `timeline.addMarker` | ✅ Full* | ✅ Enhanced | *Requires Timeline package 2.0+ |
| **Editor Operations** |
| `editor.executeMenu` | ✅ Full | ✅ Full | `EditorApplication.ExecuteMenuItem()` stable |
| `editor.selection.applyTag` | ✅ Full | ✅ Full | `GameObject.tag` unchanged |
| `editor.selection.applyLayer` | ✅ Full | ✅ Full | `GameObject.layer` unchanged |
| **Unity API & Package Search** |
| `unityApi.search` | ✅ Full | ✅ Full | Documentation index API stable |
| `unityPackages.search` | ✅ Full | ✅ Full | `PackageManager.Client.List()` stable since 2018 |
| **Batch Operations** |
| `batch.setProperty` | ✅ Full | ✅ Full | `SerializedObject` batching identical |
| `ui.createDocument` | ✅ Full | ✅ Full | None | UI Toolkit built-in API |
| `ui.addStyle` | ✅ Full | ✅ Full | None | UI Toolkit built-in API |
| `ui.bindData` | ✅ Full | ✅ Full | None | UI Toolkit built-in API |

**Legend:**
- ✅ Full = Complete feature parity, no version differences
- ✅ Enhanced = Unity 6 has optional improvements, but 2022.3 fully supported
- ✅ Full* = Requires Timeline package 2.0+ (not Unity version dependent)

### Per-Tool Version Details

#### GameObject/Component Tools - ✅ No Version Issues

**Status**: Zero conditional compilation needed. All APIs unchanged since Unity 2017.

- **`go.create`**: `new GameObject(name, Type[])` - Unchanged since Unity 2017
- **`go.delete`**: `Object.DestroyImmediate(go)` - Unchanged
- **`go.setTransform`**: `Transform.position/rotation/scale` - Unchanged
- **`component.add`**: `gameObject.AddComponent(Type.GetType(type))` - Unchanged
- **`component.remove`**: `Object.DestroyImmediate()` - Unchanged
- **`component.setProperty`**: `SerializedObject.FindProperty(path)` - Property paths identical across versions

#### File Operations - ✅ No Version Issues

**Status**: Pure .NET APIs, zero Unity version dependency.

- **`project.listFiles`**: `Directory.GetFiles(root, pattern)` - .NET Standard 2.1 stable
- **`project.readFile/writeFile`**: `File.ReadAllText/WriteAllText` - Encoding identical
- **`project.searchText`**: `Regex.Match(content, pattern)` - Stable
- **`project.inspectScript`**: Regex-based parsing (Roslyn unavailable in Unity Editor)

#### Prefab/ScriptableObject Tools - ⚠️ Minor Unity 6 Enhancement

**`prefab.editProperty`**:
- **Unity 2022.3**: `SerializedObject` on prefab asset → Standard override workflow
- **Unity 6.0+**: Optional `PrefabUtility.ApplyPrefabInstance()` improvements

**Implementation Pattern**:
```csharp
#if UNITY_6000_0_OR_NEWER
    PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
    AssetDatabase.SaveAssets();
#else
    PrefabUtility.ApplyPrefabInstance(prefabInstance);
    AssetDatabase.SaveAssets();
#endif
```

**Status**: Unity 2022.3 fully supported, Unity 6 provides optional enhancements.

#### AudioMixer Tools - ✅ No Reflection Workarounds Needed

**Status**: Zero version differences. All public APIs since Unity 2017.3.

- **`audio.mixer.createGroup`**: `mixer.CreateAudioMixerGroup(name, parent)` - Public API since 2017.3
- **`audio.mixer.createSnapshot`**: `mixer.FindSnapshot(name)` - Public API
- **`audio.mixer.setRouting`**: `audioSource.outputAudioMixerGroup = group` - Direct assignment

**Note**: No reflection workarounds required. All AudioMixer APIs are public and stable.

#### Timeline Tools - ⚠️ Package Dependency

**Status**: Requires Timeline package 2.0+. Document "Requires Timeline package" in tool descriptions.

- **Unity 2022.3**: Full support via Timeline package 2.0+
- **Unity 6.0+**: Enhanced track binding APIs (`director.SetGenericBinding` improvements)

**Implementation Pattern**:
```csharp
#if UNITY_6000_0_OR_NEWER
    // Unity 6: Enhanced track creation with auto-binding
    TrackAsset track = trackType switch {
        "animation" => timeline.CreateTrack<AnimationTrack>(trackName, true),
        "audio" => timeline.CreateTrack<AudioTrack>(trackName, true),
        _ => timeline.CreateTrack<ActivationTrack>(trackName, true)
    };
    
    // Enhanced binding API
    director.SetGenericBinding(track, targetObject, true); // Auto-create binding
#else
    // Unity 2022.3: Standard API
    TrackAsset track = trackType switch {
        "animation" => timeline.CreateTrack<AnimationTrack>(trackName),
        "audio" => timeline.CreateTrack<AudioTrack>(trackName),
        _ => timeline.CreateTrack<ActivationTrack>(trackName)
    };
    
    // Standard binding API
    director.SetGenericBinding(track, targetObject);
#endif
```

#### Editor/Package Manager Tools - ✅ Stable APIs

**Status**: All APIs unchanged across versions.

- **`editor.executeMenu`**: `EditorApplication.ExecuteMenuItem(path)` - Unchanged
- **`editor.selection.applyTag/layer`**: `GameObject.tag/layer` - Unchanged
- **`unityPackages.search`**: `PackageManager.Client.List(true, false)` - Stable since Unity 2018

### Package Requirements

**Timeline Package** (for `timeline.*` tools only):
- **Package ID**: `com.unity.timeline`
- **Minimum Version**: 2.0.0
- **Required For**: `timeline.createTrack`, `timeline.bindObject`, `timeline.addMarker`
- **Installation**: Via Unity Package Manager (Window → Package Manager → Unity Registry)

**All Other Pro Tools**: Zero package dependencies. Use only Unity built-in APIs.

### Conditional Compilation Patterns

#### Timeline Tools (TimelineTools.cs)

**Example: `timeline.createTrack`**
```csharp
[McpTool("timeline.createTrack", tier: Tier.Pro)]
public static string CreateTrack(string timelinePath, string trackType, string trackName) {
    var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
    if (timeline == null) return ErrorJson("Timeline not found", "NOT_FOUND");
    
#if UNITY_6000_0_OR_NEWER
    // Unity 6: Enhanced track creation with optional auto-binding
    TrackAsset track = trackType switch {
        "animation" => timeline.CreateTrack<AnimationTrack>(trackName, true),
        "audio" => timeline.CreateTrack<AudioTrack>(trackName, true),
        "activation" => timeline.CreateTrack<ActivationTrack>(trackName, true),
        _ => throw new ArgumentException("Invalid track type")
    };
#else
    // Unity 2022.3: Standard track creation
    TrackAsset track = trackType switch {
        "animation" => timeline.CreateTrack<AnimationTrack>(trackName),
        "audio" => timeline.CreateTrack<AudioTrack>(trackName),
        "activation" => timeline.CreateTrack<ActivationTrack>(trackName),
        _ => throw new ArgumentException("Invalid track type")
    };
#endif
    
    Undo.RecordObject(timeline, "Create Track");
    AssetDatabase.SaveAssets();
    
    return JsonUtility.ToJson(new { success = true, trackName, trackType });
}
```

#### Prefab Tools (PrefabTools.cs)

**Example: `prefab.editProperty`**
```csharp
[McpTool("prefab.editProperty", tier: Tier.Pro)]
public static string EditProperty(string prefabPath, string componentType, string propertyPath, object value) {
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    if (prefab == null) return ErrorJson("Prefab not found", "NOT_FOUND");
    
    using var so = new SerializedObject(prefab);
    var prop = so.FindProperty(propertyPath);
    if (prop == null) return ErrorJson("Property not found", "NOT_FOUND");
    
    SafeSetProperty(prop, value);
    so.ApplyModifiedProperties();
    
#if UNITY_6000_0_OR_NEWER
    // Unity 6: Enhanced prefab instance handling
    PrefabUtility.ApplyPrefabInstance(prefab, InteractionMode.AutomatedAction);
    AssetDatabase.SaveAssets();
#else
    // Unity 2022.3: Standard prefab application
    PrefabUtility.ApplyPrefabInstance(prefab);
    AssetDatabase.SaveAssets();
#endif
    
    return JsonUtility.ToJson(new { success = true });
}
```

### Asset Store Submission Notes

- ✅ **2022.3 LTS minimum** = Approved (meets Asset Store requirements)
- ✅ **Unity 6 support** = Marketing bonus (broader compatibility)
- ✅ **Timeline package note** = Clear dependency documentation
- ✅ **No breaking changes** = Zero reviewer friction
- ✅ **Minimal conditional compilation** = Clean codebase (only 2 conditional blocks, ~50 LOC total)

### Version Testing Recommendations

**Recommended Test Matrix**:
- Unity 2022.3.20f1 LTS (minimum version)
- Unity 6000.0.0f1 LTS (latest Unity 6)
- Timeline package 2.0.0 (minimum required)
- Timeline package latest (current version)

**Critical Test Scenarios**:
1. Timeline tools with Timeline package installed
2. Timeline tools without Timeline package (graceful failure)
3. Prefab tools with Unity 6 enhanced APIs
4. All AudioMixer tools (verify no reflection needed)

## Safety Guarantees

All Pro tier tools follow these safety principles:

1. **Single-Object Scope**: Pro tools operate on single objects or single assets, not project-wide
2. **Undo Support**: All write operations use Unity's Undo system for safe rollback
3. **Path Validation**: All file/asset paths are validated via `SafePaths.IsAssetsSafe()`
4. **Whitelist Enforcement**: Menu commands and file operations are restricted to safe whitelists
5. **Batch Limits**: Batch operations are capped at 50 assets to prevent long-running operations
6. **GUID Preservation**: Asset modifications use `SafeSerializedEditor` pattern to preserve GUIDs

## Performance Bounds

### Time Guards
- **Batch operations**: Limited to 50 assets maximum to ensure reasonable completion time
- **File operations**: `maxBytes` limits prevent memory/time issues
- Future versions (v1.1+) may add optional `timeoutMs` fields for heavy operations

### Deterministic Ordering
All tool outputs are designed to be deterministic:
- Arrays are sorted by stable keys (e.g., path, name)
- Dictionary keys are sorted alphabetically where applicable
- Ensures consistent JSON output across invocations

## Best-Effort Limitations

Some tools operate with "best-effort" limitations due to Unity API constraints:

- **Timeline tools**: Require Timeline package 2.0+ to be installed; operations fail gracefully if package is missing
- **Component operations**: Some component types may not be fully accessible via serialization system
- **Prefab tools**: Unity 6 provides optional enhanced prefab override handling, but 2022.3 fully supported

**Note**: AudioMixer tools use only public APIs (no reflection workarounds required). All AudioMixer operations are stable across Unity 2022.3+ and Unity 6.0+.

These limitations are documented in tool-specific notes and communicated via error messages when applicable.

