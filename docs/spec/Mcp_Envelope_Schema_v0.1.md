# MCP Envelope Schema v0.1

This document describes the request/response envelope format and error model for EditorMCP. The envelope schema follows MCP (Model Context Protocol) conventions while being tailored for Unity Editor integration.

## Overview

EditorMCP uses JSON-RPC 2.0 as the base protocol with MCP-specific extensions. All communication is JSON-based and follows a request/response pattern.

## Request Envelope

### Structure

```json
{
  "jsonrpc": "2.0",
  "id": "string|number",
  "method": "string",
  "params": {
    "tool": "string",
    "arguments": {}
  }
}
```

### Fields

#### jsonrpc (required, string)

Protocol version identifier. Must be `"2.0"`.

---

#### id (required, string|number)

Request identifier. Used to correlate requests with responses. Must be unique within a session.

**Constraints**:
- Can be a string or number
- Client-generated
- Should be unique per request

---

#### method (required, string)

The MCP method to invoke. For tool execution, this is always `"tools/call"`.

**Valid Methods**:
- `"tools/call"` - Execute a tool
- `"tools/list"` - List available tools
- `"tools/describe"` - Get tool schema
- `"server/info"` - Get server information

---

#### params (required, object)

Method-specific parameters.

**For `tools/call`**:
```json
{
  "tool": "string (required)",
  "arguments": {
    "parameterName": "value"
  }
}
```

- `tool` (required, string): The tool ID to execute
- `arguments` (required, object): Tool-specific input parameters

**For `tools/list`**:
```json
{
  "category": "string (optional)",
  "tier": "string (optional)"
}
```

**For `tools/describe`**:
```json
{
  "tool": "string (required)"
}
```

**For `server/info`**:
```json
{}
```

---

### Request Examples

**Tool Execution**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-001",
  "method": "tools/call",
  "params": {
    "tool": "scene.hierarchy.dump",
    "arguments": {
      "scenePath": "Assets/Scenes/Main.unity"
    }
  }
}
```

**List Tools**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-002",
  "method": "tools/list",
  "params": {
    "category": "scene"
  }
}
```

**Describe Tool**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-003",
  "method": "tools/describe",
  "params": {
    "tool": "project.info"
  }
}
```

---

## Response Envelope

### Success Response

```json
{
  "jsonrpc": "2.0",
  "id": "string|number",
  "result": {
    "tool": "string",
    "output": {}
  }
}
```

### Fields

#### jsonrpc (required, string)

Protocol version identifier. Must be `"2.0"`.

---

#### id (required, string|number)

The request ID from the corresponding request. Used to correlate responses with requests.

---

#### result (required, object)

The result of the method execution.

**For `tools/call`**:
```json
{
  "tool": "string",
  "output": {},
  "diagnostics": ["string"] (optional)
}
```

- `tool` (required, string): The tool ID that was executed
- `output` (required, object): Tool-specific output data
- `diagnostics` (optional, array of strings): Warnings, notes, or performance information. Used to communicate best-effort limitations, partial results, or performance bounds. May be `null` or omitted if no diagnostics are present.

**For `tools/list`**:
```json
{
  "tools": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "category": "string",
      "safetyLevel": "string",
      "tier": "string"
    }
  ]
}
```

**For `tools/describe`**:
```json
{
  "tool": {
    "id": "string",
    "name": "string",
    "description": "string",
    "category": "string",
    "safetyLevel": "string",
    "tier": "string",
    "inputs": {},
    "outputs": {},
    "notes": "string"
  }
}
```

**For `server/info`**:
```json
{
  "serverVersion": "string",
  "unityVersion": "string",
  "platform": "string",
  "enabledToolCategories": ["string"],
  "tier": "string"
}
```

---

### Error Response

```json
{
  "jsonrpc": "2.0",
  "id": "string|number",
  "error": {
    "code": "integer",
    "message": "string",
    "data": {
      "tool": "string (optional)",
      "errorType": "string",
      "details": {}
    }
  }
}
```

### Error Fields

#### code (required, integer)

JSON-RPC error code.

**Standard Codes**:
- `-32700` - Parse error
- `-32600` - Invalid Request
- `-32601` - Method not found
- `-32602` - Invalid params
- `-32603` - Internal error

**EditorMCP Extended Codes**:
- `-32000` - Tool execution error
- `-32001` - Tool not found
- `-32002` - Invalid tool arguments
- `-32003` - Tool execution timeout
- `-32004` - Unity Editor error
- `-32005` - Permission denied (tier restriction)

---

#### message (required, string)

Human-readable error message.

**Examples**:
- `"Tool execution failed"`
- `"Invalid scene path"`
- `"Tool not available in current tier"`

---

#### data (optional, object)

Additional error information.

**Structure**:
```json
{
  "tool": "string (optional)",
  "errorType": "string",
  "details": {}
}
```

**errorType Values**:
- `"validation"` - Input validation failed
- `"execution"` - Tool execution failed
- `"unity"` - Unity Editor error
- `"permission"` - Tier or permission restriction
- `"timeout"` - Operation timed out
- `"not_found"` - Resource not found

**details** (object): Tool-specific error details

---

### Response Examples

**Success - Tool Execution**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-001",
  "result": {
    "tool": "scene.hierarchy.dump",
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
}
```

**Success - Tool Execution with Diagnostics**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-002",
  "result": {
    "tool": "project.references.missing",
    "output": {
      "missingScripts": [
        {
          "path": "Assets/Scenes/Test.unity",
          "gameObjectPath": "Player",
          "componentIndex": 1,
          "context": "Missing script component at index 1 on GameObject 'Player'"
        }
      ],
      "brokenReferences": []
    },
    "diagnostics": [
      "Scan stopped after 15000ms. Processed 50 of 200 items. Results may be partial."
    ]
  }
}
```

**Error - Invalid Arguments**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-001",
  "error": {
    "code": -32002,
    "message": "Invalid tool arguments: scenePath is required",
    "data": {
      "tool": "scene.hierarchy.dump",
      "errorType": "validation",
      "details": {
        "missingParameters": ["scenePath"]
      }
    }
  }
}
```

**Error - Tool Not Found**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-002",
  "error": {
    "code": -32001,
    "message": "Tool not found: invalid.tool.id",
    "data": {
      "tool": "invalid.tool.id",
      "errorType": "not_found",
      "details": {}
    }
  }
}
```

**Error - Unity Editor Error**:
```json
{
  "jsonrpc": "2.0",
  "id": "req-003",
  "error": {
    "code": -32004,
    "message": "Unity Editor error: Scene file not found",
    "data": {
      "tool": "scene.hierarchy.dump",
      "errorType": "unity",
      "details": {
        "unityError": "Scene file 'Assets/Scenes/Missing.unity' does not exist",
        "scenePath": "Assets/Scenes/Missing.unity"
      }
    }
  }
}
```

---

## Error Handling

### Validation Errors

Occur when request parameters do not match the tool's input schema.

**Response**:
- Code: `-32002` (Invalid tool arguments)
- Error type: `"validation"`
- Details: Include missing parameters, type mismatches, or constraint violations

---

### Execution Errors

Occur when tool execution fails due to runtime conditions.

**Response**:
- Code: `-32000` (Tool execution error)
- Error type: `"execution"`
- Details: Tool-specific error information

---

### Unity Editor Errors

Occur when Unity Editor operations fail (e.g., scene not found, asset missing).

**Response**:
- Code: `-32004` (Unity Editor error)
- Error type: `"unity"`
- Details: Unity-specific error message and context

---

### Permission Errors

Occur when a tool is not available in the current tier.

**Response**:
- Code: `-32005` (Permission denied)
- Error type: `"permission"`
- Details: Required tier and current tier

---

### Timeout Errors

Occur when tool execution exceeds the timeout limit.

**Response**:
- Code: `-32003` (Tool execution timeout)
- Error type: `"timeout"`
- Details: Timeout duration and tool ID

---

## Protocol Notes

### Idempotency

Tool execution should be idempotent where possible. Read-only tools (v0.1) are inherently idempotent.

### Timeouts

- Default timeout: 30 seconds
- Long-running tools should provide progress updates (future enhancement)
- Tools with scanning operations implement time guards to prevent hangs:
  - `project.references.missing`: 15 seconds max
  - `project.assets.summary`: 10 seconds max
- When time limits are exceeded, tools return partial results with diagnostic messages

### Diagnostics

Tools may include a `diagnostics` field in their response to communicate:
- **Performance warnings**: Time limits exceeded, partial results
- **Best-effort limitations**: Unity API constraints, missing packages
- **Informational notes**: Context about tool behavior or output interpretation

Diagnostics are always informational and do not indicate errors. They provide transparency about tool behavior and limitations.

### Batch Requests

Batch requests are not supported in v0.1. Each request must be sent individually.

### Notifications

Notifications (requests without `id`) are not supported in v0.1. All requests require responses.

---

## Version Information

- **Schema Version**: 0.1.0
- **EditorMCP Core Version**: 0.1.0
- **JSON-RPC Version**: 2.0
- **Last Updated**: 2024-01-01

---

## Compliance

This schema is designed to be:
- **MCP-Compatible**: Follows MCP conventions where applicable
- **JSON-RPC 2.0 Compliant**: Base protocol compliance
- **Unity-Specific**: Tailored for Unity Editor integration
- **Extensible**: Designed for future enhancements without breaking changes

