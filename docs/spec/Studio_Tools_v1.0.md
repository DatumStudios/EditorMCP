# EditorMCP Studio Tools v1.0

This document defines the canonical list of Studio tier tools for EditorMCP v1.0. Studio tier tools extend Pro tier capabilities with **team-scale batch operations**, **scene management**, and **cross-scene workflows**. These tools have higher blast radius than Pro tier single-object operations and are designed for teams with version control and proper safety practices.

All Studio tools use existing infrastructure:
- `UndoScope` for session-level undo support
- `MainThreadDispatcher` for async operations
- `LicenseManager.Tier.Studio` for tier enforcement
- `SafePaths.IsAssetsSafe()` for path validation
- `ErrorJson` helper for standardized error responses

## File Organization

Studio tools are organized into category-specific files under the unified `Editor/EditorMcp/Tools/` directory. These files are **shared across tiers** with tier gating via `[McpTool]` attribute:

```
Editor/EditorMcp/Tools/
├── SceneTools.cs (scene.load, scene.save, scene.setActive, scene.batchApply) - Studio tier
├── PrefabTools.cs (prefab.batchReplace) - SHARED: Pro (prefab.editProperty) + Studio (prefab.batchReplace)
├── AudioTools.cs (audio.mixer.validateRouting, audio.mixer.batchRoute) - SHARED: Pro (audio.mixer.createGroup) + Studio (batch operations)
├── TimelineTools.cs (timeline.sectionMarkers, timeline.batchBind) - SHARED: Pro (timeline.createTrack) + Studio (batch operations)
├── CinemachineTools.cs (cinemachine.createVirtualCamera, cinemachine.batchAdjust, cinemachine.timelineBind) - Studio tier
├── AsmdefTools.cs (asmdef.validateGraph, asmdef.addReference) - Studio tier
├── ConventionsTools.cs (conventions.enforceEditorConfig) - Studio tier
├── ArchitectureTools.cs (project.architectureSummary) - Studio tier
├── ArtTools.cs (material.createVariant, material.batchConvert, texture.atlas, texture.resizeBatch, mesh.autoLOD, mesh.combineStatic) - Studio tier
├── BuildTools.cs (build.sizeReport, build.textureUsage) - Studio tier (separate from ArtTools.cs - analysis vs creation)
└── ProjectTools.cs (project.references.fixMissing) - Studio tier
```

**Shared Category Files Pattern:**
- `PrefabTools.cs`, `AudioTools.cs`, and `TimelineTools.cs` contain both Pro and Studio tools
- Tier gating is enforced via `[McpTool("tool.id", tier: Tier.Studio)]` attribute
- This matches Asset Store best practices (Odin, PlayMaker pattern)

## Tool Categories

- **Scene Management** (3): Scene state management and batch scene operations
- **Batch & Cross-Scene Operations** (2): Large-scale batch operations across multiple assets
- **Audio Automation** (2): Batch audio routing and validation
- **Timeline Automation** (2): Advanced Timeline batch operations
- **Cinemachine Automation** (3): Virtual camera creation, batch adjustments, and Timeline binding
- **Architecture & Validation** (4): Assembly definition management and coding standards
- **Art & Asset Pipeline** (8): Material variants, texture atlasing, LOD generation, build reporting
- **Project References** (1): Guided missing reference fixes

**Total Studio Tools**: 25 tools (adds to Core 18 + Pro 29 = 72 total tools)

**Package Dependencies**: 
- Timeline package 2.0+ (for `timeline.*` tools)
- Cinemachine package (`com.unity.cinemachine`) for `cinemachine.*` tools

---

## Scene Management Tools

### scene.load

**Purpose**: Loads a scene into the Unity Editor. Enables programmatic scene switching for team workflows where scenes are switched frequently (10x/day vs indies 1x/week).

**File**: `SceneTools.cs`

**Unity APIs**: `UnityEditor.SceneManagement.EditorSceneManager.OpenScene()`, `UnityEditor.SceneManagement.OpenSceneMode`

**Inputs**:
```json
{
  "scenePath": "Assets/Scenes/Main.unity",
  "mode": "Single" | "Additive" | "AdditiveWithoutLoading",
  "autoSaveModified": true
}
```

- `scenePath` (required, string): Path to the scene file (e.g., "Assets/Scenes/Main.unity")
- `mode` (optional, string): Scene loading mode. Default: "Single"
- `autoSaveModified` (optional, boolean): If true, calls `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` before loading. Default: true

**Outputs**:
```json
{
  "success": true,
  "scenePath": "Assets/Scenes/Main.unity",
  "sceneName": "Main",
  "isLoaded": true
}
```

**Validation Rules**:
- Scene file must exist
- Scene path must be valid Unity scene path
- Mode must be valid OpenSceneMode value

**Error Handling**:
- Scene not found: `success = false`, `error = "Scene file not found"`
- Invalid mode: `success = false`, `error = "Invalid scene loading mode"`
- Load failed: `success = false`, `error = "Failed to load scene"`
- User cancelled save: `success = false`, `error = "User cancelled save"` (if `autoSaveModified: true` and user cancels dialog)

**Safety Note**: Modifies active scene state. Uses `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` to prompt for unsaved changes if `autoSaveModified: true`. Teams should have version control in place. Higher risk than single-object operations.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `EditorSceneManager.OpenScene()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### scene.save

**Purpose**: Saves the currently active scene or a specific scene. Enables programmatic scene saving for automation workflows.

**File**: `SceneTools.cs`

**Unity APIs**: `UnityEditor.SceneManagement.EditorSceneManager.SaveScene()`, `UnityEditor.SceneManagement.SaveSceneMode`

**Inputs**:
```json
{
  "scenePath": "Assets/Scenes/Main.unity",
  "saveAs": false
}
```

- `scenePath` (optional, string): Path to scene to save. If omitted, saves active scene.
- `saveAs` (optional, boolean): If true and scenePath provided, saves as new scene. Default: false

**Outputs**:
```json
{
  "success": true,
  "scenePath": "Assets/Scenes/Main.unity",
  "saved": true
}
```

**Validation Rules**:
- Scene must be loaded or scenePath must exist
- Cannot save to read-only paths

**Error Handling**:
- Scene not found: `success = false`, `error = "Scene not found"`
- Save failed: `success = false`, `error = "Failed to save scene"`
- Read-only path: `success = false`, `error = "Cannot save to read-only path"`

**Safety Note**: Writes scene files to disk. Potential data loss if scene has unsaved changes. Teams should have version control and backup practices. Uses `EditorSceneManager.SaveScene()` with proper error handling.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `EditorSceneManager.SaveScene()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### scene.setActive

**Purpose**: Sets the active scene for multi-scene workflows. Enables programmatic active scene management.

**File**: `SceneTools.cs`

**Unity APIs**: `UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene()`

**Inputs**:
```json
{
  "scenePath": "Assets/Scenes/Main.unity"
}
```

- `scenePath` (required, string): Path to scene to set as active

**Outputs**:
```json
{
  "success": true,
  "activeScenePath": "Assets/Scenes/Main.unity"
}
```

**Validation Rules**:
- Scene must be loaded
- Scene must be valid Unity scene

**Error Handling**:
- Scene not loaded: `success = false`, `error = "Scene not loaded"`
- Scene not found: `success = false`, `error = "Scene not found"`

**Safety Note**: Changes active scene state. May affect editor selection and context. Lower risk than load/save but still requires team awareness.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `EditorSceneManager.SetActiveScene()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Batch & Cross-Scene Operations Tools

### scene.batchApply

**Purpose**: Applies an operation across multiple scenes. Enables batch operations on GameObjects across scene boundaries.

**File**: `SceneTools.cs`

**Unity APIs**: `EditorSceneManager`, `SceneManager.LoadSceneAsync()`, `GameObject`, `SerializedObject`, `Undo`

**Inputs**:
```json
{
  "scenes": ["Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity"],
  "operation": "setLayer" | "setTag" | "applyPrefabOverride" | "clearConsole",
  "params": {
    "layer": "Default",
    "tag": "Enemy",
    "componentType": "UnityEngine.BoxCollider",
    "recursive": true
  },
  "maxScenes": 10,
  "continueOnError": true
}
```

- `scenes` (required, array): List of scene paths to process
- `operation` (required, string): Operation type. Studio tier supports: "setLayer", "setTag", "applyPrefabOverride", "clearConsole"
- `params` (required, object): Operation-specific parameters
  - For "setLayer": `layer` (string), `recursive` (boolean)
  - For "setTag": `tag` (string), `recursive` (boolean)
  - For "applyPrefabOverride": `propertyPath` (string, e.g., "m_Materials.Array.data[0]")
  - For "clearConsole": No params (clears Editor.log only via `EditorUtility.ClearConsole()`)
  - **Matching logic**: `tag` OR `layer` OR `componentType` (priority order: tag > layer > componentType)
- `maxScenes` (optional, number): Maximum scenes to process. Default: 10
- `continueOnError` (optional, boolean): If true, skip failed scenes and continue. Default: true

**Outputs**:
```json
{
  "success": true,
  "scenesProcessed": 2,
  "objectsAffected": 156,
  "summary": {
    "completed": 2,
    "failed": 0
  },
  "errors": []
}
```

**Validation Rules**:
- All scene paths must exist
- Operation must be valid Studio tier operation
- `maxScenes` must not exceed 10
- Operation parameters must match operation type
- Matching parameters (tag/layer/componentType) are optional but at least one should be provided

**Error Handling**:
- Scene not found: If `continueOnError: true`, skips scene and adds to errors array. If false, returns `success = false`
- Invalid operation: `success = false`, `error = "Invalid operation: {operation}"`
- Max scenes exceeded: `success = false`, `error = "Max scenes limit exceeded"`
- Operation failed: If `continueOnError: true`, returns success with error in `errors` array. If false, returns `success = false`

**Implementation Notes**:
- Uses `UndoScope("scene.batchApply")` for session-level undo
- Loads scenes via `EditorSceneManager.OpenScene()` in additive mode
- **Matching logic**: Finds GameObjects matching tag OR layer OR componentType (priority: tag > layer > componentType)
- Applies operation to matching GameObjects:
  - "setLayer": Sets `GameObject.layer` via `LayerMask.NameToLayer()`
  - "setTag": Sets `GameObject.tag`
  - "applyPrefabOverride": Applies specific property override via `PrefabUtility.ApplyPropertyOverride()`
  - "clearConsole": Calls `EditorUtility.ClearConsole()` (Editor.log only, not scene objects)
- Saves scenes after processing
- If `continueOnError: true`, backs up scene state before processing and restores on failure

**Safety Note**: High blast radius operation affecting multiple scenes and objects. Uses session-level undo. Limited to 10 scenes per operation. Teams must have version control. Operation is logged for audit.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `SceneManager.LoadSceneAsync()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### prefab.batchReplace

**Purpose**: Replaces prefab instances with new prefab while preserving transforms and overrides. Enables large-scale prefab replacement across scenes.

**File**: `PrefabTools.cs`

**Unity APIs**: `PrefabUtility.InstantiatePrefab()`, `PrefabUtility.GetPropertyModifications()`, `PrefabUtility.IsPartOfPrefabInstance()`, `EditorSceneManager`, `Undo`

**Inputs**:
```json
{
  "oldPrefabPath": "Assets/Prefabs/OldEnemy.prefab",
  "newPrefabPath": "Assets/Prefabs/NewEnemy.prefab",
  "scenePaths": ["Assets/Scenes/Level1.unity"],
  "maxInstances": 100,
  "preserveOverrides": true
}
```

- `oldPrefabPath` (required, string): Path to old prefab asset to replace
- `newPrefabPath` (required, string): Path to new prefab asset
- `scenePaths` (required, array): List of scene paths to process
- `maxInstances` (optional, number): Maximum instances to replace. Default: 100. Studio limit: 100, Enterprise limit: 500
- `preserveOverrides` (optional, boolean): Preserve prefab instance overrides. Default: true

**Outputs**:
```json
{
  "success": true,
  "oldPrefab": "Assets/Prefabs/OldEnemy.prefab",
  "newPrefab": "Assets/Prefabs/NewEnemy.prefab",
  "replacedCount": 42,
  "preservedOverrides": 15
}
```

**Validation Rules**:
- Both prefab paths must exist and pass `SafePaths.IsAssetsSafe()`
- All scene paths must exist
- `maxInstances` must not exceed tier limit (100 for Studio, 500 for Enterprise)
- Nested prefabs not supported in v1.0 (validated via `PrefabUtility.IsPartOfPrefabInstance()`)

**Error Handling**:
- Prefab not found: `success = false`, `error = "Prefab not found: {path}"`
- Scene not found: `success = false`, `error = "Scene not found: {path}"`
- Limit exceeded: `success = false`, `error = "Max instances limit exceeded"`
- Unsafe path: `success = false`, `error = "Unsafe path detected"`
- Nested prefab detected: `success = false`, `error = "Nested prefabs not supported in v1.0"`

**Implementation Notes**:
- Uses `UndoScope("prefab.batchReplace")` for session-level undo
- **Production pattern (2022.3+)**:
  1. Capture overrides via `PrefabUtility.GetPropertyModifications(instance)`
  2. Replace with new prefab using `PrefabUtility.InstantiatePrefab(newPrefab, parent)`
  3. Transfer transform (position/rotation/scale) from old instance
  4. Reapply compatible overrides via name-matching property modifications
  5. Destroy old instance via `Undo.DestroyObjectImmediate()`
- Preserves transform and component overrides when `preserveOverrides: true`
- Compatible overrides are matched by property name between old and new prefab structures

**Safety Note**: High blast radius affecting multiple prefab instances across scenes. Uses session-level undo. All paths validated via `SafePaths.IsAssetsSafe()`. Teams must have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: Uses `PrefabUtility.InstantiatePrefab()` pattern (replaces obsolete `ReplacePrefab()`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Audio Automation Tools

### audio.mixer.validateRouting

**Purpose**: Validates AudioSource → AudioMixerGroup connections across scenes. Checks that all AudioSources are properly routed to expected mixer groups.

**File**: `AudioTools.cs`

**Unity APIs**: `AudioSource.outputAudioMixerGroup`, `AudioMixer.FindMatchingGroups()`, `GameObject.FindObjectsOfType<AudioSource>()`, `AssetDatabase`

**Inputs**:
```json
{
  "mixerPath": "Assets/Master.mixer",
  "groups": ["Master/SFX", "Master/Music"]
}
```

- `mixerPath` (required, string): Path to AudioMixer asset
- `groups` (optional, array): Expected mixer group paths to validate against

**Outputs**:
```json
{
  "success": true,
  "validCount": 45,
  "invalidCount": 3,
  "orphans": [
    {
      "source": "Enemies/Goblin/AudioSource",
      "expectedGroup": "Master/SFX",
      "actual": null
    }
  ]
}
```

**Validation Rules**:
- Mixer must exist
- All AudioSources in active scenes must have `outputAudioMixerGroup` assigned
- Groups must exist in mixer (validated via `mixer.FindMatchingGroups(groupName)`)
- No circular routing (future Enterprise feature)

**Error Handling**:
- Mixer not found: `success = false`, `error = "Mixer not found"`
- Invalid group path: Returns success with `invalidCount` and orphan details
- Scene not loaded: Returns success with warning in issues array

**Implementation Notes**:
- Scans all AudioSources in loaded scenes
- Validates `outputAudioMixerGroup` assignment
- Checks group existence via `AudioMixer.FindMatchingGroups()`
- Reports orphaned AudioSources (no group assignment)

**Safety Note**: Read-only validation operation. No modifications made. Safe for automated checks and pre-commit hooks.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: AudioMixer APIs unchanged since Unity 2017.3
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### audio.mixer.batchRoute

**Purpose**: Batch routes multiple AudioSources to AudioMixerGroups. Enables automated audio routing setup across scenes.

**File**: `AudioTools.cs`

**Unity APIs**: `AudioSource.outputAudioMixerGroup`, `AudioMixer.FindMatchingGroups()`, `Undo.RecordObject()`, `HierarchyResolver`

**Inputs**:
```json
{
  "mixerPath": "Assets/Master.mixer",
  "routes": [
    {
      "sourcePath": "Enemies/Goblin",
      "groupPath": "Master/SFX"
    },
    {
      "sourcePath": "Music/Player",
      "groupPath": "Master/Music"
    }
  ],
  "maxRoutes": 50
}
```

- `mixerPath` (required, string): Path to AudioMixer asset (e.g., "Assets/Master.mixer"). Required to resolve mixer groups.
- `routes` (required, array): Array of route definitions
  - `sourcePath` (required, string): Hierarchy path to AudioSource GameObject
  - `groupPath` (required, string): Path to AudioMixerGroup within mixer (e.g., "Master/SFX")
- `maxRoutes` (optional, number): Maximum routes to process. Default: 50. Studio limit: 50

**Outputs**:
```json
{
  "success": true,
  "routedCount": 2,
  "failed": 0,
  "details": [
    {
      "sourcePath": "Enemies/Goblin",
      "groupPath": "Master/SFX",
      "routed": true
    }
  ]
}
```

**Validation Rules**:
- Mixer path must exist and pass `SafePaths.IsAssetsSafe()`
- All source paths must exist in active scene
- All group paths must exist in mixer (pre-validated via `mixer.FindMatchingGroups()`)
- `maxRoutes` must not exceed 50
- Each route must have valid AudioSource component

**Error Handling**:
- Mixer not found: `success = false`, `error = "Mixer not found: {mixerPath}"`
- Source not found: Returns success with `routed: false` for specific route
- Group not found: Returns success with `routed: false` and error message
- Max routes exceeded: `success = false`, `error = "Max routes limit exceeded"`
- No AudioSource component: Returns success with `routed: false` for specific route

**Implementation Notes**:
- Loads mixer via `AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath)`
- Pre-validates all group paths exist before processing via `mixer.FindMatchingGroups()`
- Uses `HierarchyResolver.FindByPath()` to locate AudioSource GameObjects
- Uses `Undo.RecordObject()` for each AudioSource modification
- Batch operation with safety limit
- **Multi-mixer support**: Explicit `mixerPath` parameter resolves ambiguity when multiple mixers exist

**Safety Note**: Modifies AudioSource routing. Uses `Undo.RecordObject()` for undo support. Batch operation limited to 50 routes. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: AudioMixer APIs unchanged since Unity 2017.3
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Timeline Automation Tools

### timeline.sectionMarkers

**Purpose**: Generates markers dividing timeline into sections at regular intervals. Enables automated Timeline sectioning for cutscenes and sequences.

**File**: `TimelineTools.cs`

**Unity APIs**: `TimelineAsset`, `SignalTrack`, `SignalEmitter`, `TimelineAsset.CreateMarker()`, `AssetDatabase`

**Inputs**:
```json
{
  "timelinePath": "Assets/Cutscene.playable",
  "sectionLength": 5.0,
  "markerPrefix": "Section"
}
```

- `timelinePath` (required, string): Path to Timeline asset
- `sectionLength` (required, number): Length of each section in seconds (e.g., 5.0 = marker every 5 seconds)
- `markerPrefix` (optional, string): Prefix for marker names. Default: "Section"

**Outputs**:
```json
{
  "success": true,
  "markersAdded": 8,
  "sections": [
    {
      "start": 0,
      "end": 5,
      "clips": 2
    },
    {
      "start": 5,
      "end": 10,
      "clips": 1
    }
  ]
}
```

**Validation Rules**:
- Timeline must exist
- Section length must be positive
- Timeline package must be installed

**Error Handling**:
- Timeline not found: `success = false`, `error = "Timeline not found"`
- Invalid section length: `success = false`, `error = "Section length must be positive"`
- Timeline package not installed: `success = false`, `error = "Timeline package required (Window/Package Manager)"`
- Marker creation failed: Returns success with `markersAdded: 0` and error details

**Implementation Notes**:
- Gets timeline duration via `timeline.duration` property
- Places markers at regular intervals: 0s, 5s, 10s, 15s... (based on `sectionLength`, up to duration)
- **Auto-creates SignalTrack**: If SignalTrack doesn't exist, creates it via `timeline.CreateTrack<SignalTrack>("Markers", 0)`
- Uses `SignalTrack.CreateMarker<SignalEmitter>(time)` for each marker
- Analyzes clips within each section for reporting
- Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`

**Safety Note**: Creates markers in Timeline assets. Uses `Undo.RecordObject()` and `AssetDatabase.SaveAssets()`. Requires Timeline package 2.0+.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Timeline package 2.0+ (`com.unity.timeline`)
- **Unity 6.0+**: Enhanced marker creation APIs
- **Compatibility**: Fully supported in Unity 2022.3+, enhanced in Unity 6.0+

---

### timeline.batchBind

**Purpose**: Batch binds multiple GameObjects to Timeline tracks from a configuration. Enables automated Timeline setup from data.

**File**: `TimelineTools.cs`

**Unity APIs**: `PlayableDirector`, `TimelineAsset`, `PlayableDirector.SetGenericBinding()`, `TrackAsset`, `Undo`

**Inputs**:
```json
{
  "timelinePath": "Assets/Cutscene.playable",
  "bindings": [
    {
      "trackName": "Camera",
      "objectPath": "MainCamera"
    },
    {
      "trackName": "Player",
      "objectPath": "Player"
    }
  ],
  "maxBindings": 20
}
```

- `timelinePath` (required, string): Path to Timeline asset
- `bindings` (required, array): Array of track-to-object bindings
  - `trackName` (required, string): Name of track in Timeline
  - `objectPath` (required, string): Hierarchy path to GameObject in scene
- `maxBindings` (optional, number): Maximum bindings to process. Default: 20. Studio limit: 20

**Outputs**:
```json
{
  "success": true,
  "bound": 3,
  "failed": 0,
  "details": [
    {
      "trackName": "Camera",
      "objectPath": "MainCamera",
      "bound": true
    }
  ]
}
```

**Validation Rules**:
- Timeline must exist
- Scene must be loaded (active scene)
- All tracks must exist in Timeline
- All object paths must exist in active scene
- `maxBindings` must not exceed 20

**Error Handling**:
- Timeline not found: `success = false`, `error = "Timeline not found"`
- Scene not loaded: `success = false`, `error = "No active scene loaded"`
- Track not found: Returns success with `bound: false` for specific binding
- Object not found: Returns success with `bound: false` for specific binding
- Max bindings exceeded: `success = false`, `error = "Max bindings limit exceeded"`

**Implementation Notes**:
- Finds PlayableDirector in active scene with matching `playableAsset`
- Resolves tracks by name from Timeline asset
- Resolves GameObjects via `HierarchyResolver.FindByPath()`
- Uses `PlayableDirector.SetGenericBinding(track, gameObject)` for each binding
- Uses `Undo.RecordObject()` for undo support

**Safety Note**: Modifies PlayableDirector bindings. Uses `Undo.RecordObject()` for undo support. Limited to 20 bindings per operation. Requires Timeline package 2.0+.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Timeline package 2.0+ (`com.unity.timeline`)
- **Unity 6.0+**: Enhanced binding APIs (`SetGenericBinding` improvements)
- **Compatibility**: Fully supported in Unity 2022.3+, enhanced in Unity 6.0+

---

## Cinemachine Automation Tools

**Why Cinemachine is Studio Tier:**
- **Team camera workflows**: Teams need "Set up 10 cutscene cameras", "Batch adjust follow distances"
- **Package dependency**: `com.unity.cinemachine` (not built-in like AudioMixer/Timeline)
- **Market reality**: 65% mid-size teams use Cinemachine vs 15% solo indies
- **Studio positioning**: Pro stays "universal Unity APIs", Studio = "popular packages"
- **Market data**: Cinemachine in 40% Asset Store games, but 80% team projects

### cinemachine.createVirtualCamera

**Purpose**: Creates a Cinemachine Virtual Camera with follow and look-at targets. Enables programmatic camera setup for cutscenes and gameplay.

**File**: `CinemachineTools.cs`

**Unity APIs**: `CinemachineVirtualCamera`, `CinemachineComponentBase`, `CinemachineBrain`, `SerializedObject` (requires Cinemachine package)

**Inputs**:
```json
{
  "name": "CM_vcam_Main",
  "followTarget": "Player",
  "lookAt": "PlayerHead",
  "position": {"x": 0, "y": 2, "z": -5},
  "priority": 10,
  "lensSettings": {
    "fieldOfView": 60,
    "orthographicSize": 5
  }
}
```

- `name` (required, string): Name for the Virtual Camera GameObject
- `followTarget` (optional, string): Hierarchy path to GameObject to follow
- `lookAt` (optional, string): Hierarchy path to GameObject to look at
- `position` (optional, object): Initial position {x, y, z}
- `priority` (optional, number): Camera priority. Default: 10
- `lensSettings` (optional, object): Camera lens settings (fieldOfView or orthographicSize)

**Outputs**:
```json
{
  "success": true,
  "vcamPath": "Root/CM_vcam_Main",
  "vcamName": "CM_vcam_Main",
  "instanceId": 12345
}
```

**Validation Rules**:
- Follow and lookAt targets must exist if specified
- Scene must be loaded
- Cinemachine package must be installed (minimum version 2.8.9+)

**Error Handling**:
- Cinemachine not installed: `success = false`, `error = "Cinemachine package required (Window/Package Manager)"`
- Target not found: `success = false`, `error = "Target GameObject not found: {path}"`
- Scene not loaded: `success = false`, `error = "No active scene loaded"`

**Implementation Notes**:
- **Package detection**: Uses `PackageManager.Client.List()` to check for `com.unity.cinemachine` package
- Creates GameObject with `new GameObject($"CM_{name}")`
- Adds `CinemachineVirtualCamera` component via `AddComponent<CinemachineVirtualCamera>()`
- Sets `Follow` and `LookAt` targets via direct assignment: `vcam.Follow = transform`, `vcam.LookAt = transform`
- Uses `SerializedObject` for advanced settings (priority, lens settings)
- Registers undo via `Undo.RegisterCreatedObjectUndo(vcamGo, "Create Virtual Camera")`

**Safety Note**: Creates Virtual Camera GameObject in active scene. Uses `Undo.RegisterCreatedObjectUndo()` for undo support. Requires Cinemachine package 2.8.9+.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Cinemachine package 2.8.9+ (`com.unity.cinemachine`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### cinemachine.batchAdjust

**Purpose**: Batch adjusts multiple Cinemachine Virtual Cameras. Enables team workflows for "adjust all cutscene cameras" operations.

**File**: `CinemachineTools.cs`

**Unity APIs**: `CinemachineVirtualCamera`, `CinemachineComponentBase`, `Undo`

**Inputs**:
```json
{
  "cameras": ["CM_vcam1", "CM_vcam2", "CM_vcam3"],
  "followOffset": {"x": 0, "y": 2, "z": 0},
  "lookAtOffset": {"x": 0, "y": 1, "z": 0},
  "maxCameras": 20
}
```

- `cameras` (required, array): Names or paths of Virtual Cameras to adjust
- `followOffset` (optional, object): Offset to apply to follow target {x, y, z}
- `lookAtOffset` (optional, object): Offset to apply to look-at target {x, y, z}
- `maxCameras` (optional, number): Maximum cameras to process. Default: 20. Studio limit: 20

**Outputs**:
```json
{
  "success": true,
  "adjustedCount": 3,
  "details": [
    {
      "camera": "CM_vcam1",
      "adjusted": true
    }
  ]
}
```

**Validation Rules**:
- All camera names/paths must exist
- `maxCameras` must not exceed 20
- Cinemachine package must be installed

**Error Handling**:
- Cinemachine not installed: `success = false`, `error = "Cinemachine package not installed"`
- Camera not found: Returns success with `adjusted: false` for specific camera
- Max cameras exceeded: `success = false`, `error = "Max cameras limit exceeded"`

**Implementation Notes**:
- Finds Virtual Cameras by name or hierarchy path
- **Offset application**: Offsets are added to existing follow/lookAt offsets (not replaced)
- Uses `SerializedObject` to modify Virtual Camera properties safely
- Batch operation with safety limit

**Safety Note**: Modifies Virtual Camera settings. Uses `Undo.RecordObject()` for undo support. Batch operation limited to 20 cameras. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Cinemachine package 2.8.9+ (`com.unity.cinemachine`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### cinemachine.timelineBind

**Purpose**: Binds Cinemachine Virtual Camera to Timeline track. Enables automated cutscene camera setup.

**File**: `CinemachineTools.cs`

**Unity APIs**: `CinemachineVirtualCamera`, `PlayableDirector`, `TimelineAsset`, `CinemachineTrack`

**Inputs**:
```json
{
  "timelinePath": "Assets/Timelines/Cutscene.playable",
  "vcam": "CM_Main",
  "trackName": "Camera Track",
  "scenePath": "Assets/Scenes/Main.unity"
}
```

- `timelinePath` (required, string): Path to Timeline asset
- `vcam` (required, string): Name or path of Virtual Camera
- `trackName` (required, string): Name of Cinemachine track in Timeline
- `scenePath` (required, string): Scene containing Virtual Camera and PlayableDirector

**Outputs**:
```json
{
  "success": true,
  "bound": true,
  "timelinePath": "Assets/Timelines/Cutscene.playable",
  "vcam": "CM_Main"
}
```

**Validation Rules**:
- Timeline must exist
- Virtual Camera must exist in scene
- Cinemachine track must exist in Timeline
- Cinemachine package must be installed
- Timeline package must be installed

**Error Handling**:
- Cinemachine not installed: `success = false`, `error = "Cinemachine package not installed"`
- Timeline not found: `success = false`, `error = "Timeline not found"`
- Virtual Camera not found: `success = false`, `error = "Virtual Camera not found"`
- Track not found: `success = false`, `error = "Cinemachine track not found"`

**Safety Note**: Modifies Timeline bindings. Uses `Undo.RecordObject()` for undo support. Requires both Cinemachine and Timeline packages.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirements**: 
  - Cinemachine package (`com.unity.cinemachine`)
  - Timeline package 2.0+ (`com.unity.timeline`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Architecture & Validation Tools

### asmdef.validateGraph

**Purpose**: Validates Assembly Definition reference graph for circular dependencies and missing references. Detects dependency cycles and broken references.

**File**: `AsmdefTools.cs`

**Unity APIs**: `CompilationPipeline.GetAssemblyDependencyGraph()`, `AssemblyDefinitionAsset`, `AssetDatabase`, JSON parsing

**Inputs**:
```json
{
  "asmdefPaths": ["Assets/Scripts/Core.asmdef"]
}
```

- `asmdefPaths` (optional, array): Specific asmdefs to validate. If omitted, validates all asmdefs in project.

**Outputs**:
```json
{
  "success": true,
  "cycleCount": 0,
  "longestPath": 4,
  "warnings": []
}
```

**Validation Rules**:
- All asmdef paths must exist if specified
- All asmdefs must be valid JSON
- Graph must be parseable

**Error Handling**:
- Asmdef not found: `success = false`, `error = "Assembly definition not found: {path}"`
- Invalid asmdef JSON: Returns success with warning in `warnings` array
- Parse error: `success = false`, `error = "Failed to parse asmdef graph"`

**Implementation Notes**:
- Parses all `.asmdef` files and extracts `references` array
- Builds directed graph (A → B if A.references contains B)
- Uses Depth-First Search (DFS) to detect cycles
- Reports circular dependencies with full cycle path
- Calculates longest dependency path

**Algorithm**:
1. Parse all `.asmdef` files → extract references
2. Build directed graph (assembly → referenced assemblies)
3. DFS traversal to detect cycles
4. Report all circular dependencies with paths

**Safety Note**: Read-only validation operation. No modifications made. Safe for automated checks and pre-commit hooks.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `CompilationPipeline.GetAssemblyDependencyGraph()` stable since Unity 2018.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### asmdef.addReference

**Purpose**: Adds a reference between Assembly Definitions. Enables programmatic asmdef dependency management.

**File**: `AsmdefTools.cs`

**Unity APIs**: `AssemblyDefinitionAsset`, `AssetDatabase`, JSON parsing, file I/O

**Inputs**:
```json
{
  "sourcePath": "Assets/Scripts/Core.asmdef",
  "targetPath": "Assets/Scripts/UI.asmdef"
}
```

- `sourcePath` (required, string): Path to source asmdef (the one adding the reference)
- `targetPath` (required, string): Path to target asmdef (the one being referenced)

**Outputs**:
```json
{
  "success": true,
  "sourcePath": "Assets/Scripts/Core.asmdef",
  "targetPath": "Assets/Scripts/UI.asmdef",
  "referenceAdded": true
}
```

**Validation Rules**:
- Both asmdef paths must exist
- Target asmdef must be valid
- Reference must not already exist in source asmdef
- **Pre-validation**: Must validate no cycles created (calls `asmdef.validateGraph` first)

**Error Handling**:
- Asmdef not found: `success = false`, `error = "Assembly definition not found: {path}"`
- Invalid target: `success = false`, `error = "Invalid target assembly"`
- Reference exists: `success = false`, `error = "Reference already exists"`
- **Cycle detected**: `success = false`, `error = "Adding reference would create circular dependency"`

**Implementation Notes**:
- **Pre-validates**: Calls `asmdef.validateGraph` to check if adding reference would create cycle
- Converts target path to GUID via `AssetDatabase.AssetPathToGUID(targetPath)`
- Parses source asmdef JSON
- Adds target GUID to `references` array (if not already present)
- Writes modified JSON back to file
- Uses GUID-safe editing (preserves other fields)
- Triggers Unity recompilation

**Safety Note**: Modifies asmdef JSON files. Uses GUID-safe editing. **Pre-validates no cycles created**. Teams should have version control. May trigger recompilation.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### conventions.enforceEditorConfig

**Purpose**: Enforces EditorConfig rules across project. Validates and optionally fixes code style violations based on .editorconfig specification.

**File**: `ConventionsTools.cs`

**Unity APIs**: File I/O, `AssetDatabase.ImportAsset()`, .editorconfig parsing

**Inputs**:
```json
{
  "rules": {
    "indent_size": 4,
    "trim_trailing_whitespace": true,
    "insert_final_newline": true
  },
  "paths": ["Assets/Scripts/**/*.cs"]
}
```

- `rules` (optional, object): Rule overrides. If omitted, reads from `.editorconfig` file. Supports standard EditorConfig spec:
  - `indent_size` (number): Indentation size in spaces
  - `trim_trailing_whitespace` (boolean): Remove trailing whitespace
  - `insert_final_newline` (boolean): Ensure final newline
  - `end_of_line` (string): Line ending style ("lf", "crlf", "cr")
- `paths` (optional, array): Glob patterns for files to process. Default: `["Assets/**/*.cs"]`

**Outputs**:
```json
{
  "success": true,
  "filesFixed": 23,
  "filesChecked": 150,
  "violations": 12,
  "details": [
    {
      "file": "Assets/Scripts/MyScript.cs",
      "line": 42,
      "rule": "indent_size",
      "message": "Expected 4 spaces, found 2",
      "fixed": true
    }
  ]
}
```

**Validation Rules**:
- .editorconfig must exist if `rules` not provided
- Rules must conform to EditorConfig spec
- Path patterns must be valid glob patterns

**Error Handling**:
- No .editorconfig: `success = false`, `error = ".editorconfig not found"`
- Invalid rule: `success = false`, `error = "Invalid rule: {ruleName}"`
- Invalid path pattern: `success = false`, `error = "Invalid path pattern: {pattern}"`

**Implementation Notes**:
- Parses `.editorconfig` file (standard EditorConfig format)
- Applies rules via `AssetDatabase.ImportAsset()` → triggers Unity's formatter on reimport
- **Note**: Unity's formatter supports `indent_size`, `trim_trailing_whitespace`, `insert_final_newline`
- **Limitation**: `end_of_line` and other advanced rules may require manual file editing (reported in violations)
- Reimports C# files to apply formatting
- Reports violations and fixes applied
- Uses version control awareness (respects .gitignore)

**Rules Format**: Standard EditorConfig specification (.editorconfig)

**Safety Note**: Read-only validation by default. If rules applied, modifies C# files via reimport. Uses version control awareness. Teams should review fixes before committing.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `AssetDatabase.ImportAsset()` reimport triggers formatting
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### project.architectureSummary

**Purpose**: Generates comprehensive project architecture report including assembly structure, dependencies, asset counts, and usage statistics.

**File**: `ArchitectureTools.cs`

**Unity APIs**: `AssetDatabase.FindAssets()`, `AssemblyDefinitionAsset`, `AssetDatabase`, file I/O, reflection

**Inputs**:
```json
{}
```

No inputs required (reads project state)

**Outputs**:
```json
{
  "success": true,
  "summary": {
    "asmdefCount": 12,
    "scriptCount": 847,
    "sceneCount": 23,
    "prefabCount": 156,
    "layerUsage": {
      "Default": 156,
      "UI": 45
    },
    "tagUsage": {
      "Enemy": 12,
      "Player": 8
    },
    "coupling": {
      "Core→UI": 3,
      "UI→Core": 0
    }
  }
}
```

**Validation Rules**: None (read-only operation)

**Error Handling**: None (always succeeds, may return empty data if project is empty)

**Implementation Notes**:
- **Asmdefs**: Uses `AssetDatabase.FindAssets("t:AssemblyDefinitionAsset")` to count
- **Scripts**: Uses `project.listFiles("**/*.cs")` pattern (via internal helper)
- **Scenes**: Uses `AssetDatabase.FindAssets("t:Scene")` to count
- **Prefabs**: Uses `AssetDatabase.FindAssets("t:Prefab")` to count
- **Layer/Tag Usage**: Scans scenes and prefabs to count GameObject usage (absolute counts, not percentages)
- **Coupling**: Parses `using` statements in scripts via regex (`using\s+([\w.]+);`) + asmdef references to calculate bidirectional dependencies
  - Maps namespaces to asmdefs by matching namespace prefix to asmdef name
  - Reports both directions (A→B and B→A) for complete coupling analysis

**Data Sources**:
- Assembly definitions: `AssetDatabase.FindAssets("t:AssemblyDefinitionAsset")`
- Scripts: File system scan with `**/*.cs` pattern
- Scenes/Prefabs: `AssetDatabase.FindAssets()` with type filters
- Layer/Tag usage: Scene and prefab GameObject analysis
- Coupling: Static analysis of `using` statements and asmdef references

**Safety Note**: Read-only analysis operation. No modifications made. Safe for automated reporting.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `AssetDatabase.FindAssets()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Art & Asset Pipeline Tools

### material.batchConvert

**Purpose**: Converts multiple materials to a different shader variant. Enables batch shader migration across materials.

**File**: `ArtTools.cs`

**Unity APIs**: `Material.shader`, `Shader.Find()`, `SerializedObject`, `AssetDatabase`, `Undo`

**Inputs**:
```json
{
  "materialPaths": ["Assets/Materials/Enemy.mat", "Assets/Materials/Boss.mat"],
  "targetShader": "Universal Render Pipeline/Lit",
  "maxMaterials": 20
}
```

- `materialPaths` (required, array): List of material paths to convert
- `targetShader` (required, string): Shader name to convert to (e.g., "Universal Render Pipeline/Lit")
- `maxMaterials` (optional, number): Maximum materials to process. Default: 20. Studio limit: 20

**Outputs**:
```json
{
  "success": true,
  "convertedCount": 2,
  "errors": []
}
```

**Validation Rules**:
- All material paths must exist
- Target shader must exist (validated via `Shader.Find(targetShader)`)
- `maxMaterials` must not exceed 20
- All paths must pass `SafePaths.IsAssetsSafe()`

**Error Handling**:
- Material not found: Returns success with error in `errors` array for specific material
- Shader not found: `success = false`, `error = "Shader not found: {targetShader}"`
- Max materials exceeded: `success = false`, `error = "Max materials limit exceeded"`

**Implementation Notes**:
- Uses `UndoScope("material.batchConvert")` for session-level undo
- For each material:
  - Loads via `AssetDatabase.LoadAssetAtPath<Material>(matPath)`
  - Records undo via `Undo.RecordObject(mat, "Convert Shader")`
  - Sets shader via `mat.shader = Shader.Find(req.targetShader)`
  - **Property copying**: Uses `SerializedObject` to copy compatible properties by exact name matching
    - Properties that exist in both old and new shader are preserved
    - Properties that don't exist in target shader are discarded (logged in errors array)
    - Warns if no compatible properties found
- Saves assets via `AssetDatabase.SaveAssets()`

**Safety Note**: Modifies material shader assignments. Uses session-level undo. Limited to 20 materials per operation. Pre-validates shader exists. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `Shader.Find()` stable since Unity 3.0
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### material.createVariant

**Purpose**: Creates a Material variant from a base material with property overrides. Enables programmatic material variant creation.

**File**: `ArtTools.cs`

**Unity APIs**: `Material.Instantiate()`, `SerializedObject`, `SerializedProperty.SetProperty()`, `AssetDatabase.CreateAsset()`

**Inputs**:
```json
{
  "baseMaterialPath": "Assets/Materials/Enemy.mat",
  "variantName": "Enemy_Glowing",
  "properties": {
    "m_EmissionColor": {"r": 1, "g": 0, "b": 0, "a": 1},
    "_GlowIntensity": 2.5
  },
  "savePath": "Assets/Materials/Enemy_Glowing.mat"
}
```

- `baseMaterialPath` (required, string): Path to base material asset
- `variantName` (required, string): Name for the variant material
- `properties` (optional, object): Property overrides for variant (property name → value mapping)
- `savePath` (optional, string): Output path for variant. If omitted, uses `baseMaterialPath` directory with `variantName`.

**Outputs**:
```json
{
  "success": true,
  "variantPath": "Assets/Materials/Enemy_Glowing.mat",
  "propertiesChanged": 2
}
```

**Validation Rules**:
- Base material must exist
- Save path must pass `SafePaths.IsAssetsSafe(savePath)`
- Variant name must be unique (if savePath not provided)
- Properties must be valid for material shader

**Error Handling**:
- Base material not found: `success = false`, `error = "Base material not found"`
- Variant exists: `success = false`, `error = "Variant already exists at path"`
- Invalid property: `success = false`, `error = "Invalid property: {name}"`
- Unsafe path: `success = false`, `error = "Unsafe path detected"`

**Implementation Notes**:
- Uses `Material.Instantiate(baseMaterial)` to create variant
- Applies property overrides via `SerializedObject.SetProperty()`
- Creates asset via `AssetDatabase.CreateAsset()`
- Single material operation (not batch)

**Safety Note**: Creates new material asset. Uses `AssetDatabase.CreateAsset()`. Single material only. Path validated via `SafePaths.IsAssetsSafe()`. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: Material serialization via `SerializedObject` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### texture.atlas

**Purpose**: Creates a texture atlas from multiple textures. Enables automated texture atlas generation with UV mapping.

**File**: `ArtTools.cs`

**Unity APIs**: `Texture2D.PackTextures()`, `EditorUtility.CopySerialized()`, `Sprite`, `AssetDatabase.CreateAsset()`

**Inputs**:
```json
{
  "texturePaths": ["Assets/Textures/Enemy_Idle.png", "Assets/Textures/Enemy_Run.png"],
  "atlasName": "EnemyAtlas",
  "maxSize": 2048,
  "maxTextures": 16,
  "padding": 4
}
```

- `texturePaths` (required, array): Paths to source textures to pack
- `atlasName` (required, string): Name for the atlas (used to generate output path)
- `maxSize` (optional, number): Maximum atlas size in pixels. Default: 2048. Maximum: 4096
- `maxTextures` (optional, number): Maximum textures to pack. Default: 16. Studio limit: 16
- `padding` (optional, number): Padding between textures in pixels. Default: 4

**Outputs**:
```json
{
  "success": true,
  "atlasPath": "Assets/Textures/EnemyAtlas.png",
  "uvRects": [
    {"x": 0, "y": 0, "w": 0.5, "h": 1},
    {"x": 0.5, "y": 0, "w": 0.5, "h": 1}
  ],
  "sourceMapping": [
    {"source": "Enemy_Idle.png", "uvRect": {"x": 0, "y": 0, "w": 0.5, "h": 1}},
    {"source": "Enemy_Run.png", "uvRect": {"x": 0.5, "y": 0, "w": 0.5, "h": 1}}
  ]
}
```

**Validation Rules**:
- All texture paths must exist
- All source textures must be power-of-2 dimensions
- `maxSize` must be power of 2 and ≤ 4096
- `maxTextures` must not exceed 16
- All paths must pass `SafePaths.IsAssetsSafe()`

**Error Handling**:
- Texture not found: `success = false`, `error = "Texture not found: {path}"`
- Packing failed: `success = false`, `error = "Failed to pack textures"`
- Invalid size: `success = false`, `error = "Invalid max size (must be power of 2, ≤ 4096)"`
- Max textures exceeded: `success = false`, `error = "Max textures limit exceeded"`
- Non-power-of-2 texture: Returns success with error in details for specific texture

**Implementation Notes**:
- Loads all source textures
- Validates all are power-of-2
- Uses `Texture2D.PackTextures()` to pack textures into atlas
- Generates UV rects for each source texture
- **Output path**: Generated as `Assets/Textures/{atlasName}.png` (or directory of first texture if different)
- Creates atlas asset via `AssetDatabase.CreateAsset()`
- **Sprite creation**: Optional (not automatic). User can create sprites separately if needed

**Safety Note**: Creates new texture asset. May modify source texture import settings. Limited to 16 textures per operation. Uses `AssetDatabase.CreateAsset()`. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `Texture2D.PackTextures()` unchanged since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### texture.resizeBatch

**Purpose**: Resizes multiple textures to target dimensions. Enables batch texture resizing for optimization.

**File**: `ArtTools.cs`

**Unity APIs**: `TextureImporter`, `TextureImporter.SetReadable()`, `Texture2D.Resize()`, `AssetDatabase.ImportAsset()`

**Inputs**:
```json
{
  "texturePaths": ["Assets/Textures/UI_Icons.png"],
  "targetWidth": 256,
  "targetHeight": 256,
  "filterMode": "Bilinear",
  "maxTextures": 10
}
```

- `texturePaths` (required, array): Paths to textures to resize
- `targetWidth` (required, number): Target width in pixels
- `targetHeight` (required, number): Target height in pixels
- `filterMode` (optional, string): Resize filter mode. Default: "Bilinear". Options: "Point", "Bilinear", "Trilinear"
- `maxTextures` (optional, number): Maximum textures to process. Default: 10. Studio limit: 10

**Outputs**:
```json
{
  "success": true,
  "resizedCount": 1,
  "details": [
    {
      "texturePath": "Assets/Textures/UI_Icons.png",
      "originalSize": [512, 512],
      "newSize": [256, 256],
      "resized": true
    }
  ]
}
```

**Validation Rules**:
- All texture paths must exist
- Target dimensions must be positive
- `maxTextures` must not exceed 10
- All paths must pass `SafePaths.IsAssetsSafe()`

**Error Handling**:
- Texture not found: Returns success with `resized: false` for specific texture
- Invalid dimensions: `success = false`, `error = "Invalid target dimensions"`
- Max textures exceeded: `success = false`, `error = "Max textures limit exceeded"`

**Implementation Notes**:
- For each texture:
  - Gets `TextureImporter` via `AssetImporter.GetAtPath()`
  - Sets readable via `textureImporter.isReadable = true`
  - Reimports texture
  - Loads texture and resizes via `Texture2D.Resize(targetWidth, targetHeight)`
  - Updates texture importer settings
  - Reimports to apply changes
- **Copy location**: Creates resized copies in same directory with `_resized` suffix (e.g., `UI_Icons_resized.png`)
- Original textures are not modified (read-only operation)

**Safety Note**: Read-only operation (creates resized copies). Limited to 10 textures per operation. May create temporary assets. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `TextureImporter` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### mesh.autoLOD

**Purpose**: Generates automatic LOD levels for mesh. Enables automated LOD generation with reduction targets.

**File**: `ArtTools.cs`

**Unity APIs**: `LODGroup.SetupLODGroup()`, `MeshSimplifier` (optional package), Unity's Mesh Optimizer package (optional)

**Inputs**:
```json
{
  "meshPath": "Assets/Models/Enemy.fbx",
  "lodLevels": 3,
  "reductionTargets": [0.5, 0.25, 0.1]
}
```

- `meshPath` (required, string): Path to mesh asset
- `lodLevels` (required, number): Number of LOD levels to generate (excluding LOD0)
- `reductionTargets` (optional, array): Vertex reduction percentages for each LOD level. Default: [0.5, 0.25, 0.1] (50%, 75%, 90% reduction)

**Outputs**:
```json
{
  "success": true,
  "lodGroupPath": "Assets/Models/Enemy_LOD.prefab",
  "levels": [
    {"distance": 50, "reduction": 0.5},
    {"distance": 100, "reduction": 0.25},
    {"distance": 200, "reduction": 0.1}
  ]
}
```

**Validation Rules**:
- Mesh path must exist
- LOD levels must be positive integer
- Reduction targets must be in 0-1 range
- Number of reduction targets should match lodLevels

**Error Handling**:
- Mesh not found: `success = false`, `error = "Mesh not found"`
- Invalid LOD levels: `success = false`, `error = "Invalid LOD levels count"`
- LOD generation failed: Returns success with `levels: []` and error details
- Mesh Optimizer not available: Returns success with warning (falls back to basic LOD setup)

**Implementation Notes**:
- Loads mesh asset
- Creates LODGroup component
- Generates LOD meshes using reduction targets:
  - If Mesh Optimizer package available: Uses advanced simplification
  - Otherwise: Uses basic mesh reduction via `Mesh.CombineMeshes()` or manual simplification
- **LOD distance calculation**: Uses heuristic based on reduction level:
  - LOD1 (50% reduction): distance = 50 units
  - LOD2 (75% reduction): distance = 100 units
  - LOD3 (90% reduction): distance = 200 units
  - Formula: `distance = baseDistance * (1 / reductionLevel)` where baseDistance = 25
- Sets up LOD distances automatically based on reduction levels
- Creates prefab with LODGroup via `AssetDatabase.CreateAsset()`

**Safety Note**: Creates LOD meshes and modifies LODGroups. May create many new assets. Uses `AssetDatabase.CreateAsset()`. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `LODGroup` stable since Unity 2017.1
- **Package Note**: Mesh Optimizer package optional (enhanced simplification if available)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### mesh.combineStatic

**Purpose**: Combines static meshes into single mesh. Enables static batching optimization for performance.

**File**: `ArtTools.cs`

**Unity APIs**: `Mesh.CombineMeshes()`, `StaticBatchingUtility.Combine()`, `GameObject`, `EditorSceneManager`

**Inputs**:
```json
{
  "scenePath": "Assets/Scenes/Level1.unity",
  "staticTag": "StaticProps",
  "maxMeshes": 50
}
```

- `scenePath` (required, string): Path to scene containing static meshes
- `staticTag` (optional, string): Tag to filter GameObjects. If omitted, combines all static GameObjects
- `maxMeshes` (optional, number): Maximum meshes to combine. Default: 50. Studio limit: 50

**Outputs**:
```json
{
  "success": true,
  "combinedMeshPath": "Assets/Meshes/Level1_Static.mesh",
  "vertexCount": 12456,
  "savedDrawCalls": 23
}
```

**Validation Rules**:
- Scene path must exist
- Scene must be loadable
- `maxMeshes` must not exceed 50
- Only GameObjects marked as static will be combined

**Error Handling**:
- Scene not found: `success = false`, `error = "Scene not found"`
- No static meshes: Returns success with `vertexCount: 0` and warning
- Max meshes exceeded: `success = false`, `error = "Max meshes limit exceeded"`

**Implementation Notes**:
- Loads scene via `EditorSceneManager.OpenScene()`
- Finds all GameObjects marked as static (optionally filtered by tag)
- Collects mesh filters and renderers
- Uses `Mesh.CombineMeshes()` to combine meshes
- Creates combined mesh asset via `AssetDatabase.CreateAsset()`
- Calculates draw call savings (before vs after)
- Optionally uses `StaticBatchingUtility.Combine()` for runtime batching

**Safety Note**: Modifies scene and creates new mesh assets. Limited to 50 meshes per operation. Only processes GameObjects marked as static. Teams should have version control.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `StaticBatchingUtility.Combine()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### build.sizeReport

**Purpose**: Generates detailed build size analysis. Analyzes asset contributions to build size with breakdown by type.

**File**: `BuildTools.cs`

**Unity APIs**: `EditorBuildReport`, `BuildPipeline.BuildPlayer()`, `BuildReport`, temporary build directory

**Inputs**:
```json
{}
```

No inputs required (analyzes current project)

**Outputs**:
```json
{
  "success": true,
  "totalSizeBytes": 257163392,
  "totalSize": "245MB",
  "breakdown": {
    "textures": {"bytes": 93323264, "size": "89MB", "percentage": 36},
    "meshes": {"bytes": 47185920, "size": "45MB", "percentage": 18},
    "materials": {"bytes": 24117248, "size": "23MB", "percentage": 9},
    "scenes": {"bytes": 70254592, "size": "67MB", "percentage": 27},
    "scripts": {"bytes": 12582912, "size": "12MB", "percentage": 5},
    "other": {"bytes": 9437184, "size": "9MB", "percentage": 4}
  },
  "topAssets": [
    {
      "path": "Assets/Textures/Level1_Diffuse.png",
      "sizeBytes": 47185920,
      "size": "45MB"
    }
  ]
}
```

**Note**: `topAssets` returns top 10 assets by size. Both numeric (`sizeBytes`) and human-readable (`size`) formats provided for programmatic and display use.

**Validation Rules**: None (read-only operation)

**Error Handling**:
- Build failed: `success = false`, `error = "Build failed: {details}"`
- No build data: Returns success with estimated sizes based on asset database

**Implementation Notes**:
- Creates temporary build via `BuildPipeline.BuildPlayer()` to `Application.temporaryCachePath` or custom temp directory
- Uses current active build target (no build target parameter required)
- Analyzes `EditorBuildReport` for size breakdown
- Categorizes assets by type (textures, meshes, materials, scenes, scripts)
- Calculates percentages and identifies top 10 assets by size
- Deletes temporary build after analysis
- Falls back to asset database estimation if build not performed
- **Timeout**: Default 30 seconds (`maxExecutionMs: 30000`), cancel via timeout if exceeded

**Note**: Creates temporary build to analyze (deleted after analysis). This may take significant time.

**Safety Note**: Read-only reporting operation. Creates temporary build files (deleted after). No modifications to project. Safe for automated reporting.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `EditorBuildReport` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

### build.textureUsage

**Purpose**: Reports texture memory usage by material/shader. Helps identify texture optimization opportunities and overdraw warnings.

**File**: `BuildTools.cs`

**Unity APIs**: `AssetDatabase.FindAssets("t:Texture2D")`, `Renderer.sharedMaterials`, `TextureImporter`, `Material.GetTexture()`

**Inputs**:
```json
{}
```

No inputs required (analyzes all textures in project)

**Outputs**:
```json
{
  "success": true,
  "totalTextureMemory": "156MB",
  "usageByShader": {
    "Universal/Lit": "89MB (12 textures)",
    "Sprites/Default": "23MB (8 textures)"
  },
  "overdrawWarnings": [
    {
      "texture": "UI_Background.png",
      "pixelsOverMaxSize": 2048
    }
  ]
}
```

**Validation Rules**: None (read-only operation)

**Error Handling**: None (always succeeds, may return empty data if no textures found)

**Implementation Notes**:
- Finds all textures via `AssetDatabase.FindAssets("t:Texture2D")`
- Scans scenes and prefabs for `Renderer.sharedMaterials`
- Maps textures to shaders via `Material.GetTexture()` calls
- Calculates memory usage per shader
- Identifies oversized textures (exceeds recommended size for platform)
- Reports overdraw warnings (textures larger than necessary)

**Safety Note**: Read-only analysis operation. No modifications made. Safe for automated reporting.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: `AssetDatabase.FindAssets()` stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Project References Tools

### project.references.fixMissing

**Purpose**: Fixes missing script references project-wide. Scans scenes and prefabs for MissingScript components and applies GUID-based replacements.

**File**: `ProjectTools.cs`

**Unity APIs**: `PrefabUtility.FindPrefabRoot()`, `AssetDatabase.GUIDToAssetPath()`, `PrefabUtility.ReplacePrefab()`, scene serialization, `MissingReferenceUtility`

**Inputs**:
```json
{
  "scenes": ["Assets/Scenes/*"],
  "dryRun": true,
  "maxScenes": 20
}
```

- `scenes` (optional, array): Scene path patterns to scan. Default: `["Assets/Scenes/*"]` (all scenes)
- `dryRun` (optional, boolean): If true, reports fixes without applying. Default: true
- `maxScenes` (optional, number): Maximum scenes to process. Default: 20. Studio limit: 20

**Outputs**:
```json
{
  "success": true,
  "missingCount": 23,
  "fixedCount": 21,
  "unresolved": 2,
  "changes": [
    {
      "scene": "Level1.unity",
      "fixed": 12
    }
  ]
}
```

**Validation Rules**:
- Scene patterns must be valid glob patterns
- `maxScenes` must not exceed 20
- Replacement GUIDs must exist (if provided)

**Error Handling**:
- Invalid scene pattern: `success = false`, `error = "Invalid scene pattern: {pattern}"`
- Max scenes exceeded: `success = false`, `error = "Max scenes limit exceeded"`
- Scene load failed: Returns success with error in changes for specific scene

**Implementation Notes**:
- Scans all scenes/prefabs for `MissingScript` components (detected via `Component.GetType() == null`)
- Extracts missing script GUIDs from serialized data
- **Matching logic**: 
  - Primary: GUID lookup via `AssetDatabase.GUIDToAssetPath()` if GUID still exists
  - Fallback: Name-based matching (script class name) if GUID not found
  - Explicit mapping: User-provided GUID mapping (if provided in input)
- Applies fixes via:
  - `PrefabUtility.SavePrefabAsset()` for prefab assets
  - Scene serialization for scene objects
- Reports fixed and unresolved references
- **Disambiguation**: If multiple scripts have same name, uses first match (logs warning)

**Logic**:
1. Scan all scenes/prefabs for MissingScript components
2. Extract missing script GUIDs from serialized component data
3. Match by script GUID → replacement script:
   - Try GUID lookup first
   - Fallback to name-based matching
   - Use explicit mapping if provided
4. Apply via `PrefabUtility.SavePrefabAsset()` or scene serialization
5. Report fixed and unresolved references

**Safety Note**: Modifies assets to fix references. Uses GUID-safe editing. **Dry-run mode by default** for review. High-value operation but requires careful review. Limited to 20 scenes per operation. Teams should have version control and backups.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **API Status**: GUID system stable since Unity 2017.1
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+ (identical behavior)

---

## Version Compatibility

### Version Compatibility Matrix (All 25 Studio Tools)

| Tool | Unity 2022.3 LTS | Unity 6.0 LTS | Packages | Notes |
|------|------------------|---------------|----------|-------|
| `scene.load` | ✅ Full | ✅ Full | None | `EditorSceneManager.OpenScene()` stable |
| `scene.save` | ✅ Full | ✅ Full | None | `EditorSceneManager.SaveScene()` stable |
| `scene.setActive` | ✅ Full | ✅ Full | None | `EditorSceneManager.SetActiveScene()` stable |
| `scene.batchApply` | ✅ Full | ✅ Full | None | `SceneManager.LoadSceneAsync()` stable |
| `prefab.batchReplace` | ✅ Full | ✅ Full | None | Uses `PrefabUtility.InstantiatePrefab()` pattern (2022.3+) |
| `audio.mixer.validateRouting` | ✅ Full | ✅ Full | None | AudioMixer APIs unchanged |
| `audio.mixer.batchRoute` | ✅ Full | ✅ Full | None | AudioMixer APIs unchanged |
| `timeline.sectionMarkers` | ✅ Full* | ✅ Enhanced | Timeline 2.0+ | *Requires Timeline package |
| `timeline.batchBind` | ✅ Full* | ✅ Enhanced | Timeline 2.0+ | *Requires Timeline package |
| `cinemachine.createVirtualCamera` | ✅ Full* | ✅ Full* | Cinemachine 2.8.9+ | *Requires Cinemachine package 2.8.9+ |
| `cinemachine.batchAdjust` | ✅ Full* | ✅ Full* | Cinemachine 2.8.9+ | *Requires Cinemachine package 2.8.9+ |
| `cinemachine.timelineBind` | ✅ Full* | ✅ Enhanced* | Cinemachine 2.8.9+ + Timeline | *Requires both packages |
| `asmdef.validateGraph` | ✅ Full | ✅ Full | None | `CompilationPipeline.GetAssemblyDependencyGraph()` stable |
| `asmdef.addReference` | ✅ Full | ✅ Full | None | Asmdef JSON editing unchanged |
| `conventions.enforceEditorConfig` | ✅ Full | ✅ Full | None | `AssetDatabase.ImportAsset()` reimport stable |
| `project.architectureSummary` | ✅ Full | ✅ Full | None | `AssetDatabase.FindAssets()` stable |
| `material.createVariant` | ✅ Full | ✅ Full | None | `SerializedObject` stable |
| `material.batchConvert` | ✅ Full | ✅ Full | None | `Shader.Find()` stable |
| `texture.atlas` | ✅ Full | ✅ Full | None | `PackTextures()` stable |
| `texture.resizeBatch` | ✅ Full | ✅ Full | None | `TextureImporter` stable |
| `mesh.autoLOD` | ✅ Full | ✅ Full | MeshOptimizer? | `LODGroup` stable, Mesh Optimizer optional |
| `mesh.combineStatic` | ✅ Full | ✅ Full | None | `StaticBatchingUtility` stable |
| `build.sizeReport` | ✅ Full | ✅ Full | None | `EditorBuildReport` stable |
| `build.textureUsage` | ✅ Full | ✅ Full | None | `AssetDatabase` stable |
| `project.references.fixMissing` | ✅ Full | ✅ Full | None | GUID system stable |

**Legend:**
- ✅ Full = Complete feature parity, no version differences
- ✅ Enhanced = Unity 6 has optional improvements, but 2022.3 fully supported
- ✅ Full* = Requires Timeline package 2.0+ (not Unity version dependent)

### Safety Summary

**Batch Limits**:
- **Studio tier**: 10-50 items per operation (vs Pro: 1-10, Enterprise: 500+)
- Specific limits per tool:
  - `prefab.batchReplace`: 100 instances (Studio), 500 (Enterprise)
  - `scene.batchApply`: 10 scenes
  - `audio.mixer.batchRoute`: 50 routes
  - `timeline.batchBind`: 20 bindings
  - `material.batchConvert`: 20 materials
  - `texture.atlas`: 16 textures
  - `texture.resizeBatch`: 10 textures
  - `mesh.combineStatic`: 50 meshes
  - `project.references.fixMissing`: 20 scenes

**Safety Guarantees**:
- All write operations use `UndoScope("tool.name")` for session-level undo
- All paths validated via `SafePaths.IsAssetsSafe()`
- Pre-validation for critical operations (e.g., cycle detection before adding references, shader existence before conversion)
- Dry-run mode for destructive operations (`project.references.fixMissing`)
- Operation logging for audit trails
- **Timeout protection**: All batch operations have `maxExecutionMs: 30000` (30 seconds) default timeout
- **Package detection**: Graceful degradation with clear error messages if optional packages missing

**Infrastructure**:
- All tools use existing infrastructure: `UndoScope`, `MainThreadDispatcher`, `LicenseManager.Tier.Studio`
- Standardized error handling via `ErrorJson` helper
- GUID-safe serialization for all asset modifications

## Version Information

- **Spec Version**: 1.0.0
- **EditorMCP Studio Version**: 1.0.0
- **Last Updated**: 2024-01-01

## Notes

Studio tier tools extend Pro tier capabilities with **team-scale operations**:

- **Scene Management**: Teams switch scenes frequently (10x/day vs indies 1x/week), requiring programmatic scene state management
- **Batch Operations**: Large-scale operations across multiple assets, scenes, or prefabs
- **Cross-Scene Workflows**: Operations that span multiple scenes or the entire project
- **Architecture Tools**: Assembly definition management and coding standards enforcement
- **Art Pipeline**: Material variants, texture atlasing, LOD generation for asset optimization

**Safety Philosophy**:
- All batch operations use session-level undo (`UndoScope`)
- All modifications use GUID-safe serialization
- Teams are expected to have version control in place
- Operations are logged for audit trails
- Higher blast radius than Pro tier, but still controlled and reversible

**Upgrade Path**:
- Pro tier users upgrade to Studio for team workflows and batch operations
- Clear value proposition: "Team productivity pipeline"
- Priced at $99 to match "serious tools" like Odin Inspector and PlayMaker

Future versions may introduce additional Studio tier tools for specialized workflows.

