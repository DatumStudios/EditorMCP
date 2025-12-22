# EditorMCP Enterprise Tools v1.0

This document defines the canonical list of Enterprise tier tools for EditorMCP v1.0. Enterprise tier tools provide **CI/CD integration**, **governance and policy enforcement**, **project-wide migrations**, and **custom tool SDK** capabilities. These tools have the highest blast radius and are designed for studios with robust version control, CI pipelines, and proper safety practices.

## Tool Categories

- **CI / Build Integration** (4): Continuous integration and build automation
- **Governance & Policy** (3): Project policy enforcement and dependency auditing
- **Project-Wide Batch & Migration** (3): Large-scale project transformations
- **Addressables Management** (3): Addressables build, memory analysis, and remote catalog management
- **Localization** (2): Localization coverage and batch import
- **Custom Tool SDK** (1): Extensibility for custom tool creation

**Total Enterprise Tools**: 16 tools (adds to Studio tier for ~40 total Enterprise tools, ~150+ total across all tiers)

**Package Dependencies**: 
- Addressables package (`com.unity.addressables`) for `addressables.*` tools
- Cloud Build package (optional, for remote catalog operations)

---

## File Organization

Enterprise tools are organized into category-specific files under the unified `Editor/EditorMcp/Tools/` directory. These files are **shared across tiers** with tier gating via `[McpTool]` attribute:

```
Editor/EditorMcp/Tools/
├── EnterpriseTools.cs (ci.buildTarget, ci.runTests, ci.exportResults, runtime.callMethod) - Enterprise tier
├── GovernanceTools.cs (project.policy.check, project.policy.fix, project.dependency.audit) - Enterprise tier
├── MigrationTools.cs (yaml.safeSceneEdit, project.migratePipeline) - Enterprise tier
├── AddressablesTools.cs (addressables.buildGroups, addressables.analyzeMemory, addressables.updateRemoteCatalog) - Enterprise tier
├── LocalizationTools.cs (localization.coverageReport, localization.batchImport) - Enterprise tier
├── SDKTools.cs (sdk.registerCustomTool) - Enterprise tier
└── BatchTools.cs (batch.setProperty with Enterprise limits) - SHARED: Pro (10 items) + Studio (50 items) + Enterprise (500 items)
```

**Shared Category Files Pattern:**
- `BatchTools.cs` contains Pro, Studio, and Enterprise tools with different limits per tier
- Tier gating is enforced via `[McpTool("tool.id", tier: Tier.Enterprise)]` attribute
- This matches Asset Store best practices (Odin, PlayMaker pattern)

---

## CI / Build Integration Tools

### ci.buildTarget

**Purpose**: Sets build target and builds Unity project from command line or CI pipeline. Enables automated build processes.

**File**: `CITools.cs`

**Unity APIs**: `BuildPipeline`, `EditorUserBuildSettings`, `BuildTarget`, `BuildOptions`

**Inputs**:
```json
{
  "buildTarget": "StandaloneWindows64" | "iOS" | "Android" | "WebGL",
  "buildPath": "Builds/MyGame",
  "scenes": ["Assets/Scenes/Main.unity"],
  "options": {
    "development": false,
    "allowDebugging": false,
    "compressWithLz4": true
  }
}
```

- `buildTarget` (required, string): Target platform for build
- `buildPath` (required, string): Output path for build
- `scenes` (optional, array): Scenes to include in build. If omitted, uses build settings scenes.
- `options` (optional, object): Build options

**Outputs**:
```json
{
  "success": true,
  "buildTarget": "StandaloneWindows64",
  "buildPath": "Builds/MyGame",
  "buildSize": 52428800,
  "buildTime": 120.5,
  "buildReportPath": "Builds/MyGame/BuildReport.json"
}
```

**Validation Rules**:
- Build target must be valid and available
- Build path must be writable
- All scenes must exist

**Error Handling**:
- Invalid build target: `success = false`, `error = "Invalid or unavailable build target"`
- Build failed: `success = false`, `error = "Build failed: {details}"`
- Path not writable: `success = false`, `error = "Build path not writable"`

**Safety Note**: Performs actual Unity builds. May take significant time. Requires proper build environment setup. Should only be used in CI/CD pipelines with proper error handling and logging.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### ci.runTests

**Purpose**: Runs Unity test suite (EditMode and PlayMode tests) and returns results. Enables automated testing in CI pipelines.

**File**: `CITools.cs`

**Unity APIs**: `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi`, `NUnit.Framework`

**Inputs**:
```json
{
  "testMode": "EditMode" | "PlayMode" | "Both",
  "filter": {
    "testNames": ["MyTestClass.MyTestMethod"],
    "categories": ["Integration"]
  },
  "timeout": 300
}
```

- `testMode` (required, string): Test mode to run
- `filter` (optional, object): Test filter criteria
- `timeout` (optional, number): Timeout in seconds. Default: 300

**Outputs**:
```json
{
  "success": true,
  "testsRun": 45,
  "testsPassed": 42,
  "testsFailed": 3,
  "testsSkipped": 0,
  "duration": 12.5,
  "results": [
    {
      "testName": "MyTestClass.MyTestMethod",
      "status": "Passed",
      "duration": 0.5
    },
    {
      "testName": "MyTestClass.FailingTest",
      "status": "Failed",
      "error": "Assertion failed",
      "duration": 0.2
    }
  ]
}
```

**Validation Rules**:
- Test mode must be valid
- Filter criteria must be valid

**Error Handling**:
- Invalid test mode: `success = false`, `error = "Invalid test mode"`
- Test run timeout: `success = false`, `error = "Test run timed out"`
- Test framework not available: `success = false`, `error = "Test framework not available"`

**Safety Note**: Executes test code. Tests should be isolated and not modify project state. Requires Test Framework package. Safe for CI/CD use.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Test Framework package (`com.unity.test-framework`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### ci.exportResults

**Purpose**: Exports CI results (build reports, test results) to XML/JSON for dashboard integration. Enables CI pipeline reporting.

**File**: `CITools.cs`

**Unity APIs**: File I/O, JSON/XML serialization

**Inputs**:
```json
{
  "resultsPath": "CIResults/build-report.json",
  "format": "json" | "xml" | "junit",
  "includeDetails": true
}
```

- `resultsPath` (required, string): Output path for results file
- `format` (optional, string): Export format. Default: "json"
- `includeDetails` (optional, boolean): Include detailed results. Default: true

**Outputs**:
```json
{
  "success": true,
  "resultsPath": "CIResults/build-report.json",
  "format": "json",
  "exported": true
}
```

**Validation Rules**:
- Results path must be writable
- Format must be supported

**Error Handling**:
- Path not writable: `success = false`, `error = "Results path not writable"`
- Invalid format: `success = false`, `error = "Invalid export format"`
- No results available: `success = false`, `error = "No results to export"`

**Safety Note**: Writes results files to disk. Read-only operation on project assets. Safe for CI/CD use.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### runtime.callMethod

**Purpose**: Calls a method on a MonoBehaviour or static class in Editor-only test execution context. Enables test harnesses and runtime method invocation for testing.

**File**: `CITools.cs`

**Unity APIs**: Reflection, `MonoBehaviour`, `GameObject`

**Inputs**:
```json
{
  "typeName": "MyTestClass",
  "methodName": "RunTest",
  "parameters": [1, "test"],
  "instancePath": "Root/TestObject"
}
```

- `typeName` (required, string): Fully qualified type name
- `methodName` (required, string): Method name to call
- `parameters` (optional, array): Method parameters
- `instancePath` (optional, string): Hierarchy path to GameObject instance (for instance methods)

**Outputs**:
```json
{
  "success": true,
  "returnValue": "result",
  "executionTime": 0.05
}
```

**Validation Rules**:
- Type must exist and be accessible
- Method must exist and be callable
- Parameters must match method signature
- Instance must exist if instance method

**Error Handling**:
- Type not found: `success = false`, `error = "Type not found: {typeName}"`
- Method not found: `success = false`, `error = "Method not found: {methodName}"`
- Invalid parameters: `success = false`, `error = "Parameter mismatch"`
- Instance not found: `success = false`, `error = "Instance not found"`
- Execution failed: `success = false`, `error = "Method execution failed: {exception}"`

**Safety Note**: Executes arbitrary code via reflection. **Editor-only execution, not runtime builds**. Requires careful security boundaries. Should only be used in controlled test environments. High risk if misused.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Security Note**: Editor-only execution context. Not available in runtime builds.
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Governance & Policy Tools

### project.policy.check

**Purpose**: Checks project against naming conventions, folder structure rules, and other governance policies. Enables automated policy validation.

**File**: `GovernanceTools.cs`

**Unity APIs**: `AssetDatabase`, File I/O, regex

**Inputs**:
```json
{
  "policies": {
    "naming": {
      "scenes": "^[A-Z][a-zA-Z0-9]*\\.unity$",
      "prefabs": "^[A-Z][a-zA-Z0-9]*\\.prefab$"
    },
    "folderStructure": {
      "requiredFolders": ["Assets/Scripts", "Assets/Prefabs"],
      "forbiddenPaths": ["Assets/Temp"]
    },
    "assetSize": {
      "maxTextureSize": 4096,
      "maxAudioSize": 10485760
    }
  },
  "scope": "all" | "modified" | "selected"
}
```

- `policies` (required, object): Policy definitions
- `scope` (optional, string): Scope of assets to check. Default: "all"

**Outputs**:
```json
{
  "success": true,
  "checked": 150,
  "violations": 5,
  "details": [
    {
      "assetPath": "Assets/Scenes/main.unity",
      "policy": "naming.scenes",
      "violation": "Scene name must start with uppercase letter",
      "severity": "error"
    },
    {
      "assetPath": "Assets/Textures/LargeTexture.png",
      "policy": "assetSize.maxTextureSize",
      "violation": "Texture exceeds maximum size (8192 > 4096)",
      "severity": "warning"
    }
  ]
}
```

**Validation Rules**:
- Policies must be valid JSON
- Regex patterns must be valid
- Scope must be valid

**Error Handling**:
- Invalid policy: `success = false`, `error = "Invalid policy: {policyName}"`
- Invalid regex: `success = false`, `error = "Invalid regex pattern: {pattern}"`

**Safety Note**: Read-only validation operation. No modifications made. Safe for automated checks and pre-commit hooks.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### project.policy.fix

**Purpose**: Automatically fixes policy violations where possible. Enables automated policy enforcement.

**File**: `GovernanceTools.cs`

**Unity APIs**: `AssetDatabase`, File I/O, `AssetDatabase.RenameAsset()`, `AssetDatabase.MoveAsset()`

**Inputs**:
```json
{
  "policies": {
    "naming": {
      "scenes": "^[A-Z][a-zA-Z0-9]*\\.unity$"
    },
    "folderStructure": {
      "moveTo": {
        "Assets/Temp/*": "Assets/Scenes"
      }
    }
  },
  "dryRun": false,
  "scope": "all" | "modified" | "selected"
}
```

- `policies` (required, object): Policy definitions
- `dryRun` (optional, boolean): If true, reports fixes without applying. Default: false
- `scope` (optional, string): Scope of assets to fix. Default: "all"

**Outputs**:
```json
{
  "success": true,
  "checked": 150,
  "fixed": 3,
  "failed": 0,
  "details": [
    {
      "assetPath": "Assets/Scenes/main.unity",
      "action": "renamed",
      "newPath": "Assets/Scenes/Main.unity",
      "fixed": true
    }
  ]
}
```

**Validation Rules**:
- Policies must be valid
- Fixes must be reversible or logged
- Scope must be valid

**Error Handling**:
- Invalid policy: `success = false`, `error = "Invalid policy"`
- Fix failed: Returns success with `fixed: false` for specific asset
- Cannot fix: Returns success with `fixed: false` and reason

**Safety Note**: **Modifies project assets**. Renames files, moves assets, modifies import settings. Uses `UndoScope("project.policy.fix")` for session-level undo. **Requires version control and backups**. Should be used with `dryRun: true` first for review.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### project.dependency.audit

**Purpose**: Audits third-party packages and dependencies for licenses, security vulnerabilities, and compatibility. Enables dependency governance.

**File**: `GovernanceTools.cs`

**Unity APIs**: `PackageManager.Client`, `PackageManager.PackageInfo`, file I/O

**Inputs**:
```json
{
  "checkLicenses": true,
  "checkSecurity": true,
  "checkCompatibility": true,
  "allowedLicenses": ["MIT", "Apache-2.0"],
  "blockedPackages": ["package-name"]
}
```

- `checkLicenses` (optional, boolean): Check package licenses. Default: true
- `checkSecurity` (optional, boolean): Check for known vulnerabilities. Default: true
- `checkCompatibility` (optional, boolean): Check Unity version compatibility. Default: true
- `allowedLicenses` (optional, array): Whitelist of allowed licenses
- `blockedPackages` (optional, array): Blacklist of blocked packages

**Outputs**:
```json
{
  "success": true,
  "packagesAudited": 25,
  "issues": [
    {
      "package": "com.example.package",
      "version": "1.0.0",
      "issue": "License not in allowed list",
      "severity": "error",
      "license": "GPL-3.0"
    },
    {
      "package": "com.other.package",
      "version": "2.0.0",
      "issue": "Known security vulnerability",
      "severity": "warning",
      "cve": "CVE-2024-1234"
    }
  ],
  "summary": {
    "totalPackages": 25,
    "compliant": 22,
    "nonCompliant": 3
  }
}
```

**Validation Rules**:
- License names must be valid SPDX identifiers
- Package names must be valid

**Error Handling**:
- Package manager unavailable: `success = false`, `error = "Package Manager unavailable"`
- Invalid license format: `success = false`, `error = "Invalid license format"`

**Safety Note**: Read-only audit operation. Queries package metadata and external databases. No modifications made. Safe for automated checks.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Project-Wide Batch & Migration Tools

### yaml.safeSceneEdit

**Purpose**: Performs project-wide find/replace operations on scene YAML files with GUID preservation. Enables safe large-scale scene modifications.

**File**: `MigrationTools.cs`

**Unity APIs**: YAML parsing, `AssetDatabase`, GUID handling

**Inputs**:
```json
{
  "find": "m_Component:",
  "replace": "m_Component:",
  "pattern": "regex pattern",
  "scope": "all" | "scenes" | "prefabs",
  "preserveGuids": true
}
```

- `find` (required, string): Text or pattern to find
- `replace` (required, string): Replacement text
- `pattern` (optional, string): Regex pattern for advanced matching
- `scope` (optional, string): Scope of files to edit. Default: "all"
- `preserveGuids` (optional, boolean): Preserve GUIDs during edit. Default: true

**Outputs**:
```json
{
  "success": true,
  "filesProcessed": 45,
  "filesModified": 12,
  "replacements": 23,
  "details": [
    {
      "filePath": "Assets/Scenes/Main.unity",
      "replacements": 3,
      "modified": true
    }
  ]
}
```

**Validation Rules**:
- Find pattern must be valid
- Replace text must be safe (GUID preservation)
- Scope must be valid

**Error Handling**:
- Invalid pattern: `success = false`, `error = "Invalid regex pattern"`
- GUID corruption detected: `success = false`, `error = "GUID preservation failed"`
- File locked: Returns success with `modified: false` for specific file

**Safety Note**: **Modifies scene/prefab YAML files**. Uses GUID-safe YAML editing with validation. **High blast radius affecting entire project**. Uses `UndoScope("yaml.safeSceneEdit")` for session-level undo. **Requires version control and backups**. Should be tested on small scope first.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### project.migratePipeline

**Purpose**: Migrates project between render pipelines (Built-in → URP, URP → HDRP, etc.). Enables automated pipeline migration.

**File**: `MigrationTools.cs`

**Unity APIs**: `RenderPipelineManager`, `AssetDatabase`, `GraphicsSettings`, package management

**Inputs**:
```json
{
  "fromPipeline": "Built-in" | "URP" | "HDRP",
  "toPipeline": "URP" | "HDRP",
  "migrateMaterials": true,
  "migrateShaders": true,
  "backup": true
}
```

- `fromPipeline` (required, string): Source render pipeline
- `toPipeline` (required, string): Target render pipeline
- `migrateMaterials` (optional, boolean): Migrate materials. Default: true
- `migrateShaders` (optional, boolean): Migrate shaders. Default: true
- `backup` (optional, boolean): Create backup before migration. Default: true

**Outputs**:
```json
{
  "success": true,
  "migrationStarted": true,
  "materialsMigrated": 45,
  "shadersMigrated": 12,
  "backupPath": "Backups/Migration_20240101",
  "warnings": [
    "Some materials require manual review"
  ]
}
```

**Validation Rules**:
- Pipelines must be valid and available
- Required packages must be installed
- Project must be in valid state for migration

**Error Handling**:
- Invalid pipeline: `success = false`, `error = "Invalid pipeline: {pipeline}"`
- Package not installed: `success = false`, `error = "Required package not installed: {package}"`
- Migration failed: `success = false`, `error = "Migration failed: {details}"`

**Safety Note**: **Major project transformation**. Modifies materials, shaders, graphics settings, and potentially many assets. **Extremely high blast radius**. Creates backup by default. **Requires version control, backups, and thorough testing**. Should be performed in isolated branch first.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirements**: Target pipeline package must be installed
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Addressables Management Tools

**Why Addressables is Enterprise Tier:**
- **Build-time complexity**: BuildPipeline, ContentUpdate, remote catalogs
- **Runtime concerns**: Loading groups, memory budgets, async dependencies
- **Enterprise reality**: 90% usage in shipped games (not prototypes)
- **Tier positioning**: Studio = Editor tooling, Enterprise = deployment + asset management
- **Market data**: Addressables = production games only (5% prototypes, 75% enterprise)

### addressables.buildGroups

**Purpose**: Builds Addressables groups for production deployment. Enables automated Addressables build pipeline.

**File**: `AddressablesTools.cs`

**Unity APIs**: `AddressableAssetSettings`, `AddressableAssetSettings.BuildPlayerContent()`, `BuildPipeline`

**Inputs**:
```json
{
  "profile": "Production",
  "buildTarget": "StandaloneWindows64",
  "buildScript": "DefaultBuildScript"
}
```

- `profile` (required, string): Addressables profile name (e.g., "Production", "Development")
- `buildTarget` (optional, string): Build target platform. Default: current build target
- `buildScript` (optional, string): Build script to use. Default: "DefaultBuildScript"

**Outputs**:
```json
{
  "success": true,
  "profile": "Production",
  "buildPath": "ServerData/StandaloneWindows64",
  "catalogPath": "catalog.json",
  "buildTime": 45.2
}
```

**Validation Rules**:
- Profile must exist in Addressables settings
- Build target must be valid
- Addressables package must be installed

**Error Handling**:
- Addressables not installed: `success = false`, `error = "Addressables package not installed"`
- Profile not found: `success = false`, `error = "Profile not found: {profile}"`
- Build failed: `success = false`, `error = "Build failed: {details}"`

**Safety Note**: Performs Addressables build. May take significant time. Creates build artifacts. Requires Addressables package. Should only be used in CI/CD pipelines.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Addressables package (`com.unity.addressables`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### addressables.analyzeMemory

**Purpose**: Analyzes Addressables memory usage and loading patterns. Enables memory budget optimization for production.

**File**: `AddressablesTools.cs`

**Unity APIs**: `AddressableAssetSettings`, `ResourceManager`, memory profiling

**Inputs**:
```json
{
  "reportPath": "BuildReport.json",
  "includeDependencies": true
}
```

- `reportPath` (optional, string): Path to save analysis report. Default: "AddressablesMemoryReport.json"
- `includeDependencies` (optional, boolean): Include dependency analysis. Default: true

**Outputs**:
```json
{
  "success": true,
  "totalMemory": "245MB",
  "byGroup": {
    "UI": "89MB",
    "Audio": "45MB",
    "Textures": "111MB"
  },
  "loadingPatterns": {
    "frequent": ["UI/Buttons", "Audio/SFX"],
    "rare": ["Textures/Backgrounds"]
  }
}
```

**Validation Rules**:
- Addressables package must be installed
- Addressables groups must be configured

**Error Handling**:
- Addressables not installed: `success = false`, `error = "Addressables package not installed"`
- No groups configured: Returns success with empty data

**Safety Note**: Read-only analysis operation. No modifications made. Safe for automated reporting.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Addressables package (`com.unity.addressables`)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### addressables.updateRemoteCatalog

**Purpose**: Updates remote Addressables catalog for content updates. Enables live content updates without full builds.

**File**: `AddressablesTools.cs`

**Unity APIs**: `AddressableAssetSettings`, `ContentUpdateScript`, remote catalog management

**Inputs**:
```json
{
  "bucket": "gs://my-game-assets",
  "catalogPath": "catalog.json",
  "previousBuildPath": "ServerData/StandaloneWindows64",
  "updateGroups": ["UI", "Audio"]
}
```

- `bucket` (required, string): Remote storage bucket/path (e.g., "gs://my-game-assets", "s3://bucket")
- `catalogPath` (required, string): Path to catalog file in remote storage
- `previousBuildPath` (required, string): Path to previous build for content update comparison
- `updateGroups` (optional, array): Specific groups to update. If omitted, updates all changed groups

**Outputs**:
```json
{
  "success": true,
  "catalogUpdated": true,
  "groupsUpdated": 2,
  "remotePath": "gs://my-game-assets/catalog.json"
}
```

**Validation Rules**:
- Remote bucket/path must be accessible
- Previous build must exist
- Addressables package must be installed
- Cloud Build credentials must be configured (if using cloud storage)

**Error Handling**:
- Addressables not installed: `success = false`, `error = "Addressables package not installed"`
- Remote access failed: `success = false`, `error = "Failed to access remote storage"`
- Previous build not found: `success = false`, `error = "Previous build not found"`

**Safety Note**: Updates remote content catalogs. Affects live game content. Requires proper authentication and access controls. Should only be used in controlled deployment pipelines.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirements**: 
  - Addressables package (`com.unity.addressables`)
  - Cloud Build package (optional, for cloud storage integration)
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Localization Tools

### localization.coverageReport

**Purpose**: Generates localization coverage report analyzing which strings are localized and which are missing translations.

**File**: `LocalizationTools.cs`

**Unity APIs**: `LocalizationSettings`, `LocalizationTable`, `AssetDatabase`

**Inputs**:
```json
{
  "locales": ["en", "fr", "de"],
  "includeUnused": true
}
```

- `locales` (optional, array): Locales to analyze. If omitted, analyzes all configured locales.
- `includeUnused` (optional, boolean): Include unused translation keys. Default: true

**Outputs**:
```json
{
  "success": true,
  "totalKeys": 150,
  "coverage": {
    "en": 1.0,
    "fr": 0.85,
    "de": 0.72
  },
  "missing": [
    {
      "key": "UI.MainMenu.Start",
      "missingIn": ["de"]
    }
  ],
  "unused": [
    {
      "key": "UI.OldMenu.Title",
      "unusedIn": ["en", "fr", "de"]
    }
  ]
}
```

**Validation Rules**:
- Locales must be valid ISO codes
- Localization system must be configured

**Error Handling**:
- Localization not configured: `success = false`, `error = "Localization system not configured"`
- Invalid locale: `success = false`, `error = "Invalid locale: {locale}"`

**Safety Note**: Read-only analysis operation. No modifications made. Safe for automated reporting.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Localization package (`com.unity.localization`) if using Unity Localization
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

### localization.batchImport

**Purpose**: Batch imports localization data from CSV/Excel files. Enables automated localization workflow.

**File**: `LocalizationTools.cs`

**Unity APIs**: `LocalizationSettings`, `LocalizationTable`, CSV parsing, `AssetDatabase`

**Inputs**:
```json
{
  "importPath": "Localization/Translations.csv",
  "format": "csv" | "excel",
  "keyColumn": "Key",
  "localeColumns": {
    "en": "English",
    "fr": "French"
  },
  "overwrite": false
}
```

- `importPath` (required, string): Path to import file
- `format` (optional, string): File format. Default: "csv"
- `keyColumn` (optional, string): Column name for keys. Default: "Key"
- `localeColumns` (required, object): Mapping of locale codes to column names
- `overwrite` (optional, boolean): Overwrite existing translations. Default: false

**Outputs**:
```json
{
  "success": true,
  "keysImported": 150,
  "translationsAdded": 300,
  "translationsUpdated": 0,
  "errors": [
    {
      "row": 5,
      "key": "UI.MainMenu.Start",
      "error": "Invalid locale code"
    }
  ]
}
```

**Validation Rules**:
- Import file must exist and be readable
- Format must be supported
- Column mappings must be valid
- Locale codes must be valid

**Error Handling**:
- File not found: `success = false`, `error = "Import file not found"`
- Invalid format: `success = false`, `error = "Invalid file format"`
- Parse error: Returns success with errors array
- Invalid locale: Returns success with errors for specific rows

**Safety Note**: **Modifies localization tables**. Adds/updates translations. Uses `AssetDatabase.SaveAssets()`. **Requires version control**. Should be tested with small import first.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Package requirement**: Localization package (`com.unity.localization`) if using Unity Localization
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+

---

## Custom Tool SDK

### sdk.registerCustomTool

**Purpose**: Registers a custom MCP tool defined in user code using `[McpTool]` attribute. Enables extensibility for studios to create their own tools.

**File**: `SDKTools.cs`

**Unity APIs**: Reflection, `Assembly.GetTypes()`, `MethodInfo.GetCustomAttribute()`

**Inputs**:
```json
{
  "assemblyPath": "Assets/Scripts/MyTools.dll",
  "typeName": "MyCustomTools",
  "methodName": "MyCustomTool",
  "schema": {
    "id": "my.custom.tool",
    "description": "My custom tool",
    "inputs": {},
    "outputs": {}
  }
}
```

- `assemblyPath` (optional, string): Path to assembly containing custom tool. If omitted, scans all assemblies.
- `typeName` (optional, string): Specific type to scan
- `methodName` (optional, string): Specific method to register
- `schema` (optional, object): Manual schema override

**Outputs**:
```json
{
  "success": true,
  "toolsRegistered": 1,
  "tools": [
    {
      "id": "my.custom.tool",
      "type": "MyCustomTools",
      "method": "MyCustomTool",
      "registered": true
    }
  ]
}
```

**Validation Rules**:
- Assembly must exist if specified
- Type must exist if specified
- Method must have `[McpTool]` attribute
- Schema must be valid

**Error Handling**:
- Assembly not found: `success = false`, `error = "Assembly not found"`
- Type not found: `success = false`, `error = "Type not found"`
- Method not found: `success = false`, `error = "Method not found"`
- Missing attribute: Returns success with `registered: false` and reason
- Invalid schema: `success = false`, `error = "Invalid schema"`

**Safety Note**: Registers custom code for execution. **Requires careful security boundaries**. Custom tools execute with same permissions as EditorMCP. Studios should review custom tool code carefully. Enables powerful extensibility but requires responsibility.

**Version Notes**:
- **Minimum Unity version**: 2022.3 LTS
- **Compatibility**: Fully supported in Unity 2022.3+ and Unity 6.0+
- **SDK Documentation**: See EditorMCP SDK documentation for custom tool creation guide

---

## Version Information

- **Spec Version**: 1.0.0
- **EditorMCP Enterprise Version**: 1.0.0
- **Last Updated**: 2024-01-01

## Notes

Enterprise tier tools provide **studio infrastructure capabilities**:

- **CI/CD Integration**: Automated builds, test execution, and result reporting for continuous integration pipelines
- **Governance & Policy**: Automated policy enforcement, dependency auditing, and compliance checking
- **Project-Wide Migrations**: Large-scale project transformations (render pipeline migration, YAML edits)
- **Localization**: Automated localization workflows for multi-language projects
- **Custom Tool SDK**: Extensibility for studios to create their own MCP tools

**Safety Philosophy**:
- **Highest blast radius** - Operations affect entire projects or pipelines
- **Requires version control** - All operations assume VCS is in place
- **CI/CD focused** - Many tools designed for automated pipelines
- **Governance tools** - Policy enforcement requires careful review
- **Migration tools** - Major transformations require backups and testing
- **SDK tools** - Custom code execution requires security boundaries

**Use Cases**:
- **CI/CD Pipelines**: Automated builds and tests
- **Policy Enforcement**: Pre-commit hooks, automated compliance
- **Project Migrations**: Render pipeline upgrades, large-scale refactoring
- **Studio Tooling**: Custom tool creation for internal workflows

**Upgrade Path**:
- Studio tier users upgrade to Enterprise for CI/CD and governance
- Clear value proposition: "Studio infrastructure"
- Priced at $199 v1.0 / $299 v2.0 to reflect enterprise value and support expectations

**Critical Warnings**:
- **Migration tools** (`project.migratePipeline`, `yaml.safeSceneEdit`) can transform entire projects - always test in isolated branch first
- **Policy fix tools** (`project.policy.fix`) modify assets automatically - use `dryRun: true` for review
- **Custom SDK** enables arbitrary code execution - review custom tool code carefully
- **All Enterprise tools** assume proper version control, backups, and CI/CD practices are in place

Future versions may introduce additional Enterprise tier tools for specialized studio workflows and governance capabilities.

