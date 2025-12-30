# AI Coding Rules - EditorMCP Unity C#

## Pre-Coding Requirements

### Check for Existing Files

Before creating new files, ALWAYS search the codebase for:
- Files with similar names that might already exist
- Canonical location documents (e.g., `Development_Layout.md`)
- Existing documentation that should be updated instead of creating new files

### Canonical Location Documents

Before placing files, check these documents for canonical locations:
- `Development_Layout.md` - Repository structure, file paths, and Unity project relationships
- `Transport.md` - Transport system documentation (exists at C:\DatumStudios\)
- Other layout/architecture documents in C:\DatumStudios\

### File Placement Rules

1. **Repository is authoritative source** - All code changes must be in `C:\DatumStudios\Repos\EditorMCP\`
2. **Never edit in PackageCache** - `Library/PackageCache/` is immutable by design
3. **Check existing paths first** - Use `glob` and `grep` to find where similar files exist
4. **Update existing docs when possible** - Don't create duplicate documentation
5. **Follow namespace conventions** - File locations must match namespace structure

### Forbidden Paths

- ❌ `Library/PackageCache/` - Unity managed, changes lost
- ❌ `Library/ScriptAssemblies/` - Auto-generated, not source
- ❌ `Packages/` in Unity projects - Immutable UPM cache
- ❌ Unity test project folders - Edit in repository, not test harness

## Unity C# Scope Rules

### Class Scope Restrictions
- **Never emit executable statements at class scope** - All logic must be inside methods, constructors, or properties
- No `if`, `for`, `while`, `try` blocks outside of methods
- All braces must be balanced - run a scope audit after every edit

### Namespace Structure
- Exactly one namespace block per file
- Namespace must match the directory structure (e.g., `DatumStudios.EditorMCP.Tools`)

### Code Style
- No comments unless explicitly requested
- Follow existing code patterns and conventions
- Always check what libraries are already used in the codebase before adding new ones

---

## Tool ID Uniqueness Rules

### Tool ID Format
- Tool IDs must be unique across the entire registry
- Format: `category.action` (e.g., `go.find`, `project.info`, `mcp.server.info`)
- Defined via `[McpTool("tool.id", "description")]` attribute on static methods

### Duplicate Detection
- ToolRegistry checks for duplicate IDs during attribute-based discovery
- Duplicate tools are skipped with a warning: `"Duplicate tool ID '{id}' found. Skipping duplicate."`
- Duplicate IDs cause runtime failures - always verify uniqueness before adding new tools

### ID Assignment
- IDs must be string literals, not variables or computed values
- Empty or null IDs throw ArgumentException at compile time
- Prefix conventions:
  - `mcp.*` - Platform/core tools (server info, tools/list, tools/describe)
  - `project.*` - Project-level operations (info, assets list, references)
  - `go.*` - GameObject operations (find, create, modify)
  - `scene.*` - Scene operations (hierarchy, components, objects)
  - `asset.*` - Asset operations (info, dependencies, references)

---

## Loopback Test Patterns

### When to Use Loopback Tests
- Testing transport layer without requiring real stdio connections
- Self-test menu items (see `Window/EditorMCP/Run Self-Test`)
- Verifying request/response round-trips with in-memory streams

### Loopback Transport Implementation
```csharp
// Use in-memory MemoryStream instead of Console.OpenStandardInput()
var inputStream = new MemoryStream();
var outputStream = new MemoryStream();

// Write test request JSON to input stream
string requestJson = "{\"jsonrpc\":\"2.0\",\"id\":\"test-001\",\"method\":\"tools/call\",\"params\":{\"tool\":\"mcp.server.info\",\"arguments\":{}}}\n";
byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
inputStream.Write(requestBytes, 0, requestBytes.Length);
inputStream.Flush();
inputStream.Position = 0;

// Create loopback transport with memory streams
var transport = new LoopbackTransport(router, inputStream, outputStream);
transport.Start();

// Wait for processing
Thread.Sleep(500);

// Stop and read response
transport.Stop();
outputStream.Position = 0;
string responseJson = Encoding.UTF8.GetString(outputStream.ToArray());

// Validate response contains expected fields
bool isValid = responseJson.Contains("\"jsonrpc\"") && responseJson.Contains("\"result\"");
```

### Validation Patterns
- Verify JSON-RPC envelope: `jsonrpc`, `id`, `result` fields present
- Verify tool structure: `tool`, `output` fields in result
- Verify tool-specific output schema (e.g., `serverVersion`, `unityVersion`, `platform` for `mcp.server.info`)
- Use string contains checks for lightweight validation in tests

---

## Transport/Threading Rules

### Main Thread Dispatcher
- All Unity API calls **must** execute on the main thread
- Background transport threads (StdioTransport, LoopbackTransport) use `EditorMcpMainThreadDispatcher.Instance.Invoke()` to marshal work to main thread

### Dispatcher Usage Pattern
```csharp
// From any thread (transport layer)
var dispatcherResponse = EditorMcpMainThreadDispatcher.Instance.Invoke(() =>
{
    // This executes on Unity main thread - safe to call Unity APIs
    var registryResult = _toolRegistry.Invoke(toolId, invokeRequest);
    return registryResult.Response;
}, TimeSpan.FromSeconds(30));
```

### Dispatcher Behavior
- If caller is already on main thread: executes immediately (synchronous)
- If caller is on background thread: queues work via `EditorApplication.delayCall` and blocks until completion
- Default timeout: 30 seconds (configurable)
- Timeout returns error response with `Tool = "internal.timeout"`

### Domain Reload Safety
- Dispatcher clears work queue and unsubscribes from `delayCall` on domain reload
- Uses `InitializeOnLoadMethod` to reset state after domain reload
- Main thread ID captured once on initialization
- Main-thread check performed at entry points (Invoke), not inside delayCall callbacks

### Transport Threading
- `StdioTransport`: Runs read loop on background thread, routes via McpMessageRouter → MainThreadDispatcher
- `LoopbackTransport`: Same pattern but uses in-memory streams for testing
- All transports: Background thread reads JSON request → Router routes → MainThreadDispatcher executes tool → Response written on background thread

### Thread Safety Rules
- Never call Unity APIs directly from background transport threads
- Always use `EditorMcpMainThreadDispatcher.Instance.Invoke()` for tool execution
- Response writing happens on the transport's background thread (safe - no Unity APIs)
- Use locks (`lock (_queueLock)`) when modifying shared collections

---

## Test Transport Rules

### **Never Start Real StdioTransport in Tests**

**Critical Rule:** Real `StdioTransport` should never be instantiated in tests or self-tests.

### Why
- StdioTransport blocks on `Console.OpenStandardInput()` and `Console.OpenStandardOutput()`
- Real stdin/stdout cannot be simulated in Unity Editor
- Test suites will hang waiting for input that never comes

### Correct Test Pattern
Use `LoopbackTransport` (in-memory streams) for all testing:

```csharp
// ❌ WRONG - Will hang
var transport = new StdioTransport(router);
transport.Start(); // BLOCKS FOREVER

// ✅ CORRECT - Uses in-memory streams
var inputStream = new MemoryStream();
var outputStream = new MemoryStream();
var transport = new LoopbackTransport(router, inputStream, outputStream);
transport.Start();
```

### LoopbackTransport Characteristics
- Same logic as StdioTransport (same read/write patterns)
- Uses `MemoryStream` instead of `Console` streams
- Fully controllable in tests (write request, read response)
- No blocking on stdin/stdout

### Test Menu Item
Use `[MenuItem("Window/EditorMCP/Run Self-Test")]` for manual testing via Unity Editor menu (see `EditorMcpTransportLoopbackTest.cs`)

---

## Debugging Transport Issues

### Diagnostics Export

When troubleshooting loopback failures or transport issues, export diagnostic information:

1. Run `Tools/EditorMCP/Export Diagnostics` from Unity menu
2. This creates `Temp/EditorMCP/diagnostics.json` with:
   - Unity version and project configuration
   - Console entries (last 200: Error/Warning/Log with stack traces)
   - ToolRegistry state (discovered tools, duplicates)
   - Loaded assembly definitions

3. Attach `diagnostics.json` to bug reports or share with AI assistants for analysis

### What Diagnostics Contains

- `unityVersion` - Unity Editor version
- `timeUtc` - Export timestamp
- `buildTarget`, `scriptingBackend` - Build configuration
- `defineSymbols` - Current scripting defines
- `consoleEntries` - Console log with type, message, stack trace
- `toolRegistry.discoveredToolCount` - Number of registered tools
- `toolRegistry.toolIds` - List of all tool IDs
- `toolRegistry.duplicates` - Any duplicate tool IDs detected

### Reference
See `Transport.md` (C:\DatumStudios\) section "Diagnostics Export" for complete documentation.

---

## Lessons Learned & Reference Documents

### When to Reference These Documents

Before making architectural decisions, debugging issues, or determining file placement, consult these lessons learned documents:

### Canonical Path Documents

These documents provide authoritative guidance on where files should be placed and how projects are organized:

| Document | Location | Scope | Purpose |
|-----------|----------|-------|---------|
| `canonical_paths_v6.md` | `C:\DatumStudios\Repos\Rolling Thunder\Docs\Developer\` | Rolling Thunder (game repo) | Game root, project root, SPA architecture, folder conventions |
| `Development_Layout.md` | `C:\DatumStudios\` | EditorMCP (Unity package) | Repository structure, Git UPM mapping, Unity test harness paths |

### Lessons Learned Documents

These documents capture historical problems, solutions, and best practices learned during development:

| Document | Location | Scope | Key Insights |
|-----------|----------|-------|--------------|
| `lessons_learned_v1.txt` | `C:\DatumStudios\Repos\Rolling Thunder\Tools\Docs\` | Rolling Thunder | Unity 6.1 constraints, UI Toolkit limitations, debugging patterns, SPA architecture |
| `lessons_changelog.md` | `C:\DatumStudios\Repos\Rolling Thunder\Tools\Docs\` | Rolling Thunder | Version evolution of lessons |
| `Lessons_Learned_EditorMCP.md` | `C:\DatumStudios\` | EditorMCP | Unity 6 meta file requirements, PackageCache immutability, assembly definitions |

### Key Lessons Summary

**From Rolling Thunder (Game Repo):**

**Unity 6.1 Constraints:**
- DataContractJsonSerializer is broken for nested objects - use manual JSON parser
- Named tuple elements are compile-time hints only - use `.Item1`/`.Item2` at runtime
- Limited .NET Standard 2.1 - no System.Text.Json, no modern .NET features

**UI Toolkit Limitations:**
- No `ui:` prefixes in UXML
- No `flex: 1` - use `flex-grow: 1`
- No pseudo-classes (`:hover`, `:first-child`, etc.)
- No `transition`, `box-shadow` properties
- CSS color-mix() fails silently - use literal colors

**Architecture Patterns:**
- SPA pattern: One interface, one implementation, consumers depend on interfaces
- Strict Editor segregation: Editor scripts in `Assets/Editor/**` only
- Backend-neutral SPA signatures: No frontend types in backend interfaces
- Frozen contracts: Freeze SPA interfaces with version numbers

**Testing & Debugging:**
- Progressive debugging: Add logging, observe, identify root cause, fix
- Conditional compilation: Wrap debug logs in `#if UNITY_EDITOR && DEBUG_ANALYSIS_LOGS`
- Observable state > assumptions: Add logging before code changes
- Error pattern recognition: Know common Unity error codes (CS0101, CS0246, CS1061, etc.)

**From EditorMCP (Unity Package):**

**Unity 6 Package Constraints:**
- Meta files are MANDATORY for Git UPM packages - missing `.meta` = no compilation
- PackageCache is immutable - never edit files there, always edit in repository
- Use `?path=` parameter in Git URL to specify package subfolder

**Assembly Management:**
- Unity compiles `.cs` source to `.dll` in `Library/ScriptAssemblies/`
- Never generate DLLs manually - Unity is the compiler
- Assemblies exist in PackageCache with changing hash values

**Best Practices:**
- Source-first approach: All code is `.cs` source files, no pre-compiled DLLs
- Repository is authoritative: Edit in `C:\DatumStudios\Repos\EditorMCP\`, never in PackageCache
- Atomic file writes: Write to `.tmp`, then `File.Replace()` to prevent corruption

### When to Read These Documents

**Before starting new development:**
- Read relevant lessons learned document for your project
- Check canonical paths document for correct file placement
- Review architectural patterns (SPA, strict Editor segregation, etc.)

**When encountering bugs or issues:**
- Search lessons learned for similar problems
- Check debugging methodology section for systematic approaches
- Review error pattern recognition for compiler-specific issues

**Before refactoring:**
- Review SPA architecture patterns
- Check frozen contract discipline lessons
- Review naming conventions and namespace structure

### Quick Reference by Project Type

**For Unity Package Development (EditorMCP):**
1. `Development_Layout.md` - Repository structure and Git UPM mapping
2. `Lessons_Learned_EditorMCP.md` - Meta files, PackageCache, assembly definitions
3. `AI_Coding_Rules.md` - This document (Unity C# scope, tool IDs, threading, diagnostics)

**For Game Development (Rolling Thunder):**
1. `canonical_paths_v6.md` - Game root, project root, SPA architecture
2. `lessons_learned_v1.txt` - Unity 6.1 constraints, UI Toolkit, debugging, SPA
3. `lessons_changelog.md` - Lessons version evolution

---

## Development Workflow Rules

### Unity Doc-First Workflow

**Purpose:** Always consult version-pinned Unity documentation before implementing features

**Checklist:**
1. Detect Unity version from `ProjectSettings/ProjectVersion.txt` → minor & full tag
2. Detect relevant package versions from `Packages/manifest.json`
3. Consult version-correct docs:
   - Manual: `https://docs.unity3d.com/<MINOR>/Documentation/Manual/`
   - API: `https://docs.unity3d.com/<MINOR>/Documentation/ScriptReference/`
   - Packages: `https://docs.unity3d.com/Packages/<name>@<ver>/manual/`
4. Apply only APIs that exist in detected version
5. Add Provenance Block to responses (see below)

### Provenance Block Requirements

When implementing Unity features or making significant changes, include a Provenance Block:

```
Unity Version: <FULL> (minor: <MINOR>)
Packages: <name@x.y.z, ...>
Docs consulted:
- Manual: <exact URL(s)>
- Scripting API: <exact URL(s)>
- Package docs: <exact URL(s)>
Key notes:
- <1–3 bullets on constraints, signatures, or gotchas>
```

### Edit Over Duplicate (Default Rule)

**Prefer modifying canonical files over creating duplicates**

- Always search codebase for existing files before creating new ones
- If a similar file exists, update it instead of duplicating
- This prevents code drift and maintenance burden
- Override only when `@allow-duplication` is explicitly provided with justification

### Controlled Duplication (Opt-In)

**Allow duplication only with explicit documentation**

- Requires `@allow-duplication` tag and a documented sunset/merge plan
- Record: rationale, owner, and target consolidation date
- Example: Temporarily duplicating for parallel work with plan to merge in sprint X

### Unity Script Compilation & Test Workflow

**When editing Unity C# scripts:**

1. **Save Scripts**: Always ensure scripts are saved to disk
2. **Refresh Unity**: Allow Unity to detect file changes and compile (wait 3-5 seconds)
3. **Check for Errors**: Verify no compilation errors exist before proceeding
   - Use Unity console/logs
   - Look for compilation error indicators
4. **Only Then Test**: Never run tests until compilation is clean

**Rationale:**
- Unity MCP tools interact with live Unity Editor state
- Script changes must compile before they affect tests
- Running tests on uncompiled code produces false failures and wasted time

**Anti-Patterns (DO NOT DO):**
- ❌ Running tests immediately after script edits
- ❌ Assuming Unity auto-refreshed without verification
- ❌ Ignoring compilation errors and proceeding
- ❌ Using batch mode Unity when Editor instance is open

### Unity Test Verification Workflow

**After accepting file changes that affect tests:**

1. **ALWAYS refresh Unity console** to check for compilation errors
2. **ALWAYS rerun affected tests** using appropriate test runner
3. **ALWAYS wait for test completion** (15-20 seconds for Integration)
4. **ALWAYS read FRESH test results** from latest run directory
5. **NEVER assume old test results are still valid

**Anti-Patterns (DO NOT DO):**
- ❌ Looking at old test results without rerunning
- ❌ Assuming compilation succeeded without checking console
- ❌ Making code changes based on stale test output

---

## Unity Code Architecture Patterns

### Core Principles

- **Prefer composition and small services** over deep inheritance
- **Use ScriptableObjects** for configuration and data assets
- **Isolate editor-only code** under `Editor/` assemblies
- **Yields to Doc-First rule** for API signatures and version specifics

### Frontend/Backend Separation (For Game Projects)

**Layer Boundary Rules:**
- **Frontend must NOT reference Backend types** directly
- **Backend must NOT reference Frontend** types directly
- **Cross-layer communication via:** DTOs, ScriptableObjects, events, or boundary services

**If you need cross-layer data:**
1. Move interface to Backend and consume via DTO/event
2. Call through a boundary service
3. Keep tests isolated to their layer (use contract/integration tests for cross-layer validation)

### Core Minimalism

**Frontend/Core** and **Backend/Core** directories:
- Contain only foundational systems (bootstrap, game loop, events, registries, lightweight services)
- NO: scenes, prefabs, textures, materials, animations, audio files, shaders
- Place assets in module-specific folders

### Module README Requirements

**Each module root needs a README.md documenting:**
- Purpose of the module
- Public API (methods, events, services)
- Ownership/maintainer
- Dependencies (internal and external)

### Meta File Integrity

**Critical for Unity 6 Git UPM packages:**

- Every asset file must have a paired `.meta` file
- Keep `.meta` with its asset so Unity keeps GUIDs stable
- Missing `.meta` in Git UPM packages = compilation failure
- Never orphan `.meta` files (delete them when assets are deleted)

### Assembly Definition Health

**Prevent circular dependencies:**
- Scan asmdef references for cycles
- Break cycles by introducing interface/DTO assembly or inverting dependency
- Example: Core ← Services ← Core (cycle) → Core ← IServices ← Services

---

## Naming Conventions & Code Style

### Naming Patterns

| Type | Convention | Example |
|-------|-------------|----------|
| Classes | PascalCase | `ToolRegistry`, `AudioService` |
| Interfaces | I + PascalCase | `IAudioService`, `IGenreLibraryService` |
| Methods | PascalCase | `GetGenreIds()`, `AnalyzePath()` |
| Private Fields | _camelCase | `_descriptorCache`, `_aliasToId` |
| Public Properties | PascalCase | `IsLoaded`, `GenreCount` |
| Constants | UPPER_SNAKE_CASE | `MAX_RETRIES`, `DEFAULT_TIMEOUT` |
| Booleans | Is/Has/Can prefix | `IsLoaded`, `HasLibrosa`, `CanAnalyze` |
| Events | OnEventName | `OnTrackChanged`, `OnAnalysisComplete` |
| Folders | PascalCase for modules, lowercase for categories | `AudioAnalysis/`, `bin/`, `env/` |
| Files | kebab-case (unless language convention) | `scan_uss_colors.ps1`, `audio-service.cs` |
| CSS Classes | kebab-case | `.credits-panel`, `.btn-primary` |

### Unity Lifecycle Methods (Exact Names Required)

`Awake()`, `Start()`, `Update()`, `FixedUpdate()`, `LateUpdate()`,
`OnEnable()`, `OnDisable()`, `OnDestroy()`, `OnValidate()`, `OnDrawGizmos()`

### Editor Callbacks

`OnInspectorGUI()`, `OnSceneGUI()`, `OnEnable()`, `OnDisable()`

---

## Test-Driven Development (TDD) Rules

### Core Loop (Red → Green → Refactor)

1. **Red (spec first):** Propose/adjust tests *before* writing code
   - Include edge cases, error paths, boundary values
   - Tests must be deterministic, isolated, and fast
2. **Green (minimal change):** Implement smallest code to pass new tests
3. **Refactor (no behavior change):** Improve names, remove duplication, simplify complexity
4. **Re-run tests** to ensure no regressions

### Scope & Change Budget

- Work on **one function/class/module at a time**
- If a change touches multiple files, justify why and list each file + reason
- Never expand scope silently. Ask to split work if diff grows beyond small reviewable segment

### Test Taxonomy

- **Unit tests:** fast, pure logic; no network/disk/UI
- **Integration tests:** controlled IO; verify module boundaries and contracts
- **E2E/UI tests:** few; reserved for critical flows
- Prefer **contract tests** for boundary surfaces
- Use **golden snapshots** sparingly (only for stable text/JSON)

### Determinism & Reliability

- For randomness, set a fixed seed and assert statistical/structural properties
- Time-dependent code uses injectable clocks
- Avoid hidden global state and test interdependence

### Mocks, Fakes, Fixtures

- Mock only **external** dependencies; don't mock your own internal logic
- Keep fixtures small and named by intent (e.g., `valid_band_roster`, `empty_mod_folder`)
- Reset state between tests; no shared mutable fixtures

### Acceptance Criteria

Each cycle must specify clear pass/fail:
- Which tests should exist (names & brief intent)
- What conditions define **green**
- Any non-functional targets (performance, memory, style)

### Coverage & Quality

- Aim for **meaningful** coverage. Prioritize critical paths and invariants
- Add **regression tests** for every production bug fixed
- Keep each test under ~100 lines and each file focused on one subject

---

## Unity UI Toolkit Rules

### Theme & Tokens

- **Do NOT hardcode colors** in scene USS. Use theme tokens: `var(--app-*)`
- Keep tokens in `AppTokens.uss`; keep theme variations in exactly one `AppTheme-*.uss`
- **No layout rules inside theme files**; themes set colors/metrics only

### PanelSettings & Sheet Order

- Always include **Default (Theme Style Sheet)** first
- Only one active `AppTheme-*.uss` attached at runtime

### Unity 6.1 Supported Features

- Use **Flexbox**
- Avoid unsupported USS/CSS:
  - `display: grid`
  - 8-digit hex `#RRGGBBAA`
  - Pseudo-classes: `:hover`, `:first-child`, `:not()`, `:nth-child()`
  - CSS Properties: `transition`, `transform`, `box-shadow`, `will-change`
  - `@media queries`, `@import` statements
  - `gap`, `row-gap`, `column-gap`
- Prefer `flex-grow: 1` over `flex: 1` shorthand
- Use literal colors or `rgba()` instead of `color-mix()` (fails silently)

### UXML/USS Responsibilities

**UXML:**
- Visual element hierarchy
- Widget definitions (Button, Label, DropdownField)
- Element names/classes for CSS targeting
- Style sheet references via `<Style src="..."/>`

**USS:**
- ALL layout (flex, width, height, margin, padding)
- ALL styling (colors, fonts, borders)
- ALL states (hover, active, disabled) via USS classes

**C# Scripts:**
- Query elements by name/class (Q(), UQ())
- Hook events (RegisterCallback, clicked +=)
- Toggle CSS classes for states (AddToClassList/RemoveFromClassList)
- Data binding (populate dropdowns, update labels)

---

## PowerShell Rules (When Applicable)

### Conventions

- Use **PowerShell 7+** cmdlets: `Get-ChildItem`, `Remove-Item`, `Copy-Item`
- Avoid bash/WSL syntax: `ls`, `rm`, `cp`
- Quote all paths with spaces: `& "C:\Path With Spaces\file.ps1"`
- Use dedicated `.ps1` scripts under `Tools/Scripts/`

### Common Patterns

**File Listing:**
`Get-ChildItem -Path "Assets/Editor/Audio" -Filter "*.cs" -Recurse`

**File Deletion:**
`Remove-Item -Path "temp.txt" -Force`

**Batch Operations:**
```powershell
$files = Get-ChildItem -Filter "*.bak"
foreach ($file in $files) {
    Remove-Item -Path $file.FullName -Force
}
```

### Testing

- Unity PlayMode: `.run_test_suite.ps1 -Suite PlayMode`
- Unity EditMode: `.run_test_suite.ps1 -Suite EditMode`
- Exit codes: 0 = Success, 1 = Compile errors, 127 = Launch failure

---

## Console Error Investigation & Fixing Protocol

### Two-Phase Error Resolution

When Unity console errors are provided, follow this two-phase approach:

#### PHASE 1 - Investigation
- Read the Unity console errors
- Identify the PRIMARY root cause
- Explain the root cause in ONE paragraph
- **Do NOT write any code yet**

#### PHASE 2 - Patch Application
- Apply the MINIMAL patch required to fix the root cause
- Do NOT add features
- Do NOT refactor unrelated code
- Fix only the smallest set of lines necessary to compile
- Do not refactor unless necessary to compile

### Scope Audit After Edits
After applying any patch, mentally run a scope audit:
- No `if`, `for`, `while`, `try` statements outside methods
- All braces are balanced
- Exactly one namespace block per file
- All code compiles in Unity

### Root Cause Identification
When analyzing errors:
- Look for the specific error message and location
- Identify missing types, duplicate definitions, incorrect syntax
- Check for namespace/using directive issues
- Verify method signatures match expected patterns
- Examine surrounding context to understand intended behavior

### Minimal Fix Examples
- Missing type: Add correct using directive or fix type name
- Duplicate class: Remove duplicate definition, keep one
- Missing brace: Add the required closing brace
- Wrong return type: Change to correct existing type (e.g., `ToolInvocationResult` instead of non-existent `EditorMcpResponse`)
- Invalid statement at class scope: Move inside method

## Error Handling

### Tool Execution Errors
- Wrap tool execution in try-catch blocks
- Return `ToolInvocationResult.Failure(EditorMcpError)` on exceptions
- Use `EditorMcpErrorCodes` for error categorization (e.g., `ToolNotFound`, `ToolExecutionTimeout`)

### Transport Errors
- Log transport errors but don't crash (keep service running)
- Send parse error responses for invalid JSON
- Use `McpError` with `JsonRpcErrorCodes.ParseError` for parse failures

### Validation Errors
- Use `ToolInputValidator.Validate()` for input validation
- Return detailed error messages with field paths (e.g., `"scenePath: Required parameter is missing"`)
- Validation failures return `ToolInvocationResult.Failure(EditorMcpError)` with validation details

---

## PowerShell / Windows Command Rules

### Shell Assumptions

**Current Environment:**
- Operating System: Windows (Cursor's integrated terminal)
- Shell: PowerShell 7+
- No bash environment available

**Rule:** Always assume PowerShell on Windows unless explicitly told otherwise.

### PowerShell Command Preferences

1. **Prefer PowerShell-native commands**
   - ✅ `Get-ChildItem` instead of `ls`
   - ✅ `Get-Content` instead of `cat`
   - ✅ `Set-Content` instead of writing via `>`
   - ✅ `Remove-Item` instead of `rm`
   - ✅ `Copy-Item` instead of `cp`
   - ✅ `Move-Item` instead of `mv`

2. **Windows-specific commands**
   - ✅ `where.exe` for PATH lookup (not `which`)
   - ✅ `Select-String` for text searching (basic `grep` replacement)
   - ✅ `Test-Path` for file/directory existence checks

3. **Do NOT use bash-only syntax**
   - ❌ `$(...)` command substitution (bash)
   - ❌ `export VAR=...` (bash)
   - ❌ `VAR=value` cmd prefix (bash)
   - ❌ `sed/awk/grep` unless you explicitly detect they exist

### Quoting Rules

**PowerShell string interpolation:**
- ✅ Use double quotes for string interpolation: `$env:VAR` or `$variable`
- ✅ Use single quotes for literal strings: `'literal text'`

**Examples:**
```powershell
# String interpolation (double quotes)
"Path: $env:USERPROFILE"
"File: $filePath"

# Literal strings (single quotes)
'C:\DatumStudios\Repos\EditorMCP'
```

### Path Handling

**Prefer `-LiteralPath` for Windows paths containing `[]`:**
```powershell
# Standard path
Get-Content C:\Path\To\File.txt

# Literal path (for paths with [])
Get-Content -LiteralPath 'C:\Path\With[Brackets]\File.txt'
```

### Environment Variables

**Read environment variables:**
```powershell
$path = $env:PATH
$user = $env:USERNAME
$editor = $env:EDITOR
```

**Set for current session:**
```powershell
$env:NAME="value"
```

**Set persistently (requires new terminal):**
```powershell
# PowerShell 7+ (cross-platform)
setx NAME "value"

# Note: setx requires a new terminal session to take effect
```

### Pipelines

**Use `|` with PowerShell cmdlets:**
```powershell
# Simple filtering
Get-ChildItem | Select-String "pattern"

# Complex pipeline
Get-Content file.txt | Where-Object { $_ -match "error" } | Select-Object -First 10
```

**For simple text searching use:**
```powershell
Get-Content file.txt | Select-String "search term"
```

### JSON Handling

**Parse JSON:**
```powershell
$obj = Get-Content file.json | ConvertFrom-Json
$version = $obj.version
```

**Write JSON:**
```powershell
$obj | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 file.json
```

**Note:** `ConvertTo-Json` escapes special characters; `-Depth` controls nested object expansion.

### Pre-Command Checklist

**Before running any command:**

1. **State which shell assumptions you are making**
   - "Assuming PowerShell on Windows..."
   - "Checking for bash compatibility..."

2. **If a command might be missing, propose a fallback**
   - PowerShell equivalent
   - Cross-platform alternative (if applicable)

3. **Verify command exists before running**
   ```powershell
   if (Get-Command "Get-ChildItem" -ErrorAction SilentlyContinue) {
       Get-ChildItem ...
   }
   ```

### Common PowerShell Patterns

**List files:**
```powershell
Get-ChildItem -Path "C:\Path" -Filter "*.cs" -Recurse
```

**Read file content:**
```powershell
$content = Get-Content -Path "C:\file.txt" -Raw
```

**Check if file exists:**
```powershell
if (Test-Path "C:\file.txt") {
    # File exists
}
```

**Delete file:**
```powershell
Remove-Item -Path "C:\file.txt" -Force
```

**Get file line count:**
```powershell
(Get-Content file.txt).Count
```

---

## Additional References

### Cursor AI Rules (Rolling Thunder Project)

Some rules in this document were derived from `C:\DatumStudios\Repos\Rolling Thunder\.cursor\rules`:

| Rule | Description | Applicability |
|-------|-------------|----------------|
| Unity Doc-First Workflow | Consult version-pinned Unity docs before implementing | Unity API work |
| Provenance Blocks | Document Unity version, packages, and sources in code responses | Unity features |
| Edit Over Duplicate | Prefer modifying existing files over creating new ones | General |
| Controlled Duplication | Allow duplication only with `@allow-duplication` tag and documented plan | General |
| Script Compilation Workflow | Save → Refresh → Check Errors → Test | Unity C# |
| Test Verification Workflow | Rerun tests after changes, read fresh results | Unity testing |
| Unity Code Architecture | Composition over inheritance, ScriptableObjects, Editor isolation | Unity C# |
| Frontend/Backend Separation | No direct cross-layer references, use DTOs/events | Game projects |
| TDD Rules | Red → Green → Refactor loop with test-first approach | Testing |
| Naming Conventions | PascalCase, camelCase, UPPER_SNAKE_CASE patterns | All code |
| Module README | Each module needs README.md documenting purpose and API | Project organization |
| Meta File Integrity | Every Unity asset must have paired `.meta` file | Unity Git UPM |
| Assembly Definition Health | Prevent circular dependencies in asmdef files | Unity assemblies |
| UI Toolkit Rules | Theme tokens, 6.1 compatibility, no unsupported CSS | Unity UI Toolkit |

**Note:** Cursor AI rules are specific to Cursor's `.cursor/rules` system with priority-based conflict resolution. OpenCode adapts these rules as general development guidelines applicable to AI-assisted Unity C# development.

### Cursor Rule Priority System (For Reference)

When conflicts occur, higher priority rules supersede lower:
- 110 — Bootstrap Loader
- 100 — Global Meta
- 95 — Unity Doc-First Workflow
- 90 — Architecture (Unity code architecture patterns)
- 85 — Unity UI (UI Toolkit UXML/USS rules)
- 80 — Unity Code (language & compile-safe rules)
- 75 — Git/GitHub CLI, Controlled Duplication
- 70 — Edit Over Duplicate (default anti-duplication)
- 40 — Web-Design (Non-Unity)
- 20 — Learn From Errors / Self Improve

---

**Document Status:** Updated with PowerShell/Windows command rules (December 26, 2025)
