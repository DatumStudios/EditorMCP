# Tool Definition Schema v0.1

This document describes the schema for declaring tools in EditorMCP. Tool definitions are JSON objects that describe the tool's identity, behavior, inputs, outputs, and metadata.

## Schema Overview

Tool definitions serve multiple purposes:
- **Discovery**: Enable clients to discover available tools
- **Validation**: Provide schemas for input validation
- **Documentation**: Describe tool behavior and safety characteristics
- **Tier Gating**: Indicate which tier provides access to the tool

## Root Schema

```json
{
  "id": "string (required)",
  "name": "string (required)",
  "description": "string (required)",
  "category": "string (required)",
  "safetyLevel": "string (required)",
  "tier": "string (required)",
  "inputs": "object (required)",
  "outputs": "object (required)",
  "notes": "string (optional)",
  "examples": "array (optional)"
}
```

## Field Definitions

### id (required, string)

The canonical tool identifier. Must be unique across all tools and follow the naming convention: `category.subcategory.action` or `category.action`.

**Examples**:
- `mcp.server.info`
- `project.scenes.list`
- `scene.hierarchy.dump`
- `audio.mixer.snapshot.read`

**Constraints**:
- Must be lowercase
- Use dots (`.`) as separators
- No spaces or special characters except dots and hyphens
- Must be stable across versions (changes require version bump)

---

### name (required, string)

Human-readable display name for the tool.

**Examples**:
- `"MCP Server Info"`
- `"List Project Scenes"`
- `"Dump Scene Hierarchy"`

**Constraints**:
- Should be concise but descriptive
- Use title case
- No length limit, but prefer < 50 characters

---

### description (required, string)

Detailed description of what the tool does, its purpose, and when to use it. Should be clear enough for both humans and LLMs to understand the tool's behavior.

**Examples**:
- `"Returns server and environment information to verify the MCP bridge is operational."`
- `"Lists all scenes in the project with their paths and build settings status."`

**Constraints**:
- Should be 1-3 sentences
- Avoid implementation details
- Focus on behavior and outcomes

---

### category (required, string)

Tool category for grouping and filtering. Categories are hierarchical and use dots as separators.

**Standard Categories**:
- `mcp.platform` - Core MCP protocol tools
- `project` - Project-wide inspection
- `scene` - Scene and hierarchy operations
- `asset` - Asset inspection and analysis
- `audio` - Audio-specific tools (example domain)
- `editor` - Editor state and selection

**Constraints**:
- Must match one of the standard categories
- Use lowercase
- Use dots for hierarchy (e.g., `mcp.platform`, `audio.mixer`)

---

### safetyLevel (required, string)

Indicates the safety level of the tool. In v0.1, all tools are read-only.

**Valid Values**:
- `"read-only"` - Tool only reads data; no modifications possible

**Future Values** (not in v0.1):
- `"safe-write"` - Tool can modify data but with safety guardrails
- `"destructive"` - Tool can make irreversible changes (requires confirmation)

**Constraints**:
- Must be one of the valid values
- v0.1 tools must be `"read-only"`

---

### tier (required, string)

Indicates which tier provides access to this tool.

**Valid Values**:
- `"core"` - Available in free tier
- `"tier1"` - Available in Tier 1 (Indie)
- `"tier2"` - Available in Tier 2 (Pro)
- `"tier3"` - Available in Tier 3 (Studio)
- `"tier4"` - Available in Tier 4 (Enterprise)

**Constraints**:
- Must be one of the valid values
- v0.1 tools must be `"core"`

---

### inputs (required, object)

Schema describing the tool's input parameters. Each property represents a named parameter.

**Structure**:
```json
{
  "parameterName": {
    "type": "string (required)",
    "required": "boolean (required)",
    "description": "string (required)",
    "default": "any (optional)",
    "enum": ["array (optional)"],
    "minimum": "number (optional)",
    "maximum": "number (optional)"
  }
}
```

**Parameter Types**:
- `"string"` - Text value
- `"integer"` - Whole number
- `"number"` - Floating-point number
- `"boolean"` - True/false value
- `"array"` - Array of values (items type specified in description)
- `"object"` - Nested object (structure specified in description)

**Examples**:
```json
{
  "scenePath": {
    "type": "string",
    "required": true,
    "description": "Path to the scene file (e.g., 'Assets/Scenes/Main.unity')"
  },
  "includeInBuild": {
    "type": "boolean",
    "required": false,
    "description": "Filter by scenes included in build settings",
    "default": null
  },
  "depth": {
    "type": "integer",
    "required": false,
    "description": "Maximum depth to traverse dependency graph",
    "default": 2,
    "minimum": 1,
    "maximum": 10
  }
}
```

**Constraints**:
- Property names must be valid JSON identifiers
- `required: true` parameters must not have `default` values
- `enum` should be used when parameter has a fixed set of valid values

---

### outputs (required, object)

Schema describing the tool's output structure. Provides a contract for what the tool returns.

**Structure**:
```json
{
  "propertyName": {
    "type": "string (required)",
    "description": "string (required)",
    "items": "object (optional, for arrays)"
  }
}
```

**Output Types**:
- `"string"` - Text value
- `"integer"` - Whole number
- `"number"` - Floating-point number
- `"boolean"` - True/false value
- `"array"` - Array of values
- `"object"` - Nested object

**Examples**:
```json
{
  "scenes": {
    "type": "array",
    "description": "List of scenes in the project",
    "items": {
      "type": "object",
      "properties": {
        "path": {"type": "string"},
        "name": {"type": "string"},
        "enabledInBuild": {"type": "boolean"}
      }
    }
  }
}
```

**Constraints**:
- Should accurately represent the actual output structure
- Nested objects should define their properties
- Arrays should specify item types when possible

---

### notes (optional, string)

Additional information about the tool, including:
- Limitations or known issues
- Performance considerations
- Usage recommendations
- Warnings or important caveats

**Examples**:
- `"Large scenes may take several seconds to process."`
- `"Only detects missing references in loaded assets."`
- `"Requires scene to be saved before inspection."`

**Constraints**:
- Should be concise
- Focus on information not covered in description
- Use for safety warnings or important limitations

---

### examples (optional, array)

Example invocations of the tool with sample inputs and expected outputs.

**Structure**:
```json
[
  {
    "description": "string",
    "input": {
      "parameterName": "value"
    },
    "output": {
      "propertyName": "value"
    }
  }
]
```

**Constraints**:
- Should include 1-3 representative examples
- Examples should be realistic and useful
- Input/output should match the declared schemas

---

## Complete Example

```json
{
  "id": "scene.hierarchy.dump",
  "name": "Dump Scene Hierarchy",
  "description": "Returns the complete GameObject hierarchy for a scene, including components per node and object paths. Cornerstone tool for editor reasoning and structural analysis.",
  "category": "scene",
  "safetyLevel": "read-only",
  "tier": "core",
  "inputs": {
    "scenePath": {
      "type": "string",
      "required": true,
      "description": "Path to the scene file (e.g., 'Assets/Scenes/Main.unity')"
    }
  },
  "outputs": {
    "scenePath": {
      "type": "string",
      "description": "Path to the scene that was dumped"
    },
    "rootObjects": {
      "type": "array",
      "description": "Root GameObjects in the scene",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "path": {"type": "string"},
          "instanceId": {"type": "integer"},
          "components": {"type": "array", "items": {"type": "string"}},
          "children": {"type": "array"}
        }
      }
    }
  },
  "notes": "Large scenes with thousands of objects may take several seconds to process.",
  "examples": [
    {
      "description": "Dump hierarchy for main scene",
      "input": {
        "scenePath": "Assets/Scenes/Main.unity"
      },
      "output": {
        "scenePath": "Assets/Scenes/Main.unity",
        "rootObjects": [
          {
            "name": "Main Camera",
            "path": "Main Camera",
            "instanceId": 12345,
            "components": ["Transform", "Camera"],
            "children": []
          }
        ]
      }
    }
  ]
}
```

## Validation Rules

1. **Required Fields**: All required fields must be present and non-empty
2. **ID Uniqueness**: Tool IDs must be unique across all tools
3. **Type Consistency**: Input/output types must match their declared schemas
4. **Safety Compliance**: v0.1 tools must have `safetyLevel: "read-only"` and `tier: "core"`
5. **Category Validity**: Categories must match standard category list
6. **Schema Completeness**: Input/output schemas should be complete enough for validation

## Versioning

- **Schema Version**: 0.1.0
- **EditorMCP Core Version**: 0.1.0
- **Last Updated**: 2024-01-01

Schema changes will be versioned. Breaking changes require a new schema version.

