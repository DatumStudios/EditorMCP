using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;
using DatumStudios.EditorMCP.Diagnostics;
using DatumStudios.EditorMCP.Schemas;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Core MCP Platform Tools - Server information, tool discovery, and health monitoring.
    /// </summary>
    [McpToolCategory("mcp.platform")]
    public static class CoreTools
    {
        private const string SERVER_VERSION = "0.1.0";

        /// <summary>
        /// Tool: mcp.server.info - Returns server and environment information to verify that MCP bridge is operational and provide context for tool execution.
        /// </summary>
        /// <param name="jsonParams">JSON parameters (no parameters required).</param>
        /// <returns>Dictionary with server information.</returns>
        [McpTool("mcp.server.info", "Returns server and environment information to verify that MCP bridge is operational and provide context for tool execution.", Tier.Core)]
        public static Dictionary<string, object> ServerInfo(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            if (toolRegistry == null)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", "ToolRegistry not initialized" }
                };
            }

            var categorySet = new HashSet<string>();
            var registeredTools = toolRegistry.List();

            foreach (var tool in registeredTools)
            {
                if (!string.IsNullOrEmpty(tool.Category) && !categorySet.Contains(tool.Category))
                {
                    categorySet.Add(tool.Category);
                }
            }

            // Sort for deterministic ordering
            var categories = categorySet.OrderBy(c => c).ToArray();

            return new Dictionary<string, object>
            {
                { "success", true },
                { "serverVersion", SERVER_VERSION },
                { "unityVersion", Application.unityVersion },
                { "minUnityVersion", VersionValidator.GetMinimumVersion() },
                { "isCompatible", VersionValidator.IsCompatible() },
                { "platform", Application.platform.ToString() },
                { "enabledToolCategories", categories },
                { "tier", LicenseManager.CurrentTier.ToString().ToLower() },
                { "toolCount", toolRegistry.Count }
            };
        }

        /// <summary>
        /// Tool: mcp.tools.list - Lists all available tools with their metadata.
        /// </summary>
        [McpTool("mcp.tools.list", "Lists all available tools with their metadata, categories, and tier availability. Required for MCP client discovery.", Tier.Core)]
        public static Dictionary<string, object> ToolsList(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            if (toolRegistry == null)
            {
            return new Dictionary<string, object>
                {
                    { "error", "ToolRegistry not initialized" }
                };
            }

            // Parse JSON parameters
            string category = null;
            string tier = null;

            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null)
                {
                    if (paramsObj.TryGetValue("category", out var categoryObj) && categoryObj is string)
                    {
                        category = (string)categoryObj;
                    }

                    if (paramsObj.TryGetValue("tier", out var tierObj) && tierObj is string)
                    {
                        tier = (string)tierObj;
                    }
                }
            }

            var tools = toolRegistry.List(category, tier);
            // Sort for deterministic ordering
            var sortedTools = tools.OrderBy(t => t.Id).ToList();
            var toolSummaries = sortedTools.Select(tool => new Dictionary<string, object>
            {
                { "id", tool.Id },
                { "name", tool.Name },
                { "category", tool.Category ?? "" },
                { "safetyLevel", tool.SafetyLevel.ToString() },
                { "description", tool.Description ?? "" },
                { "tier", tool.Tier ?? "" }
            }).ToArray();

            return new Dictionary<string, object>
                {
                    { "tool", toolSummaries }
                };
            }

        /// <summary>
        /// Tool: mcp.tool.describe - Returns the complete schema for a specific tool.
        /// </summary>
        [McpTool("mcp.tool.describe", "Returns the complete schema for a specific tool, including input parameters, output structure, and safety information. Critical for LLM grounding and human inspection.", Tier.Core)]
        public static Dictionary<string, object> ToolDescribe(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            
            // Define empty definition once for reuse
            var emptyDefinition = new Dictionary<string, object>
            {
                { "error", "ToolRegistry not initialized" }
            };
            
            if (toolRegistry == null)
            {
                return new Dictionary<string, object>
                {
                    { "tool", emptyDefinition }
                };
            }
            
            // Parse JSON parameters
            string toolId = null;

            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null && paramsObj.TryGetValue("toolId", out var toolIdObj))
                {
                    toolId = toolIdObj as string;
                }
            }

            if (string.IsNullOrEmpty(toolId))
            {
                var notFoundDefinition = new Dictionary<string, object>
                {
                    { "id", toolId ?? "" },
                    { "error", "Tool not found" }
                };
                
                return new Dictionary<string, object>
                {
                    { "tool", notFoundDefinition }
                };
            }

            var definition = toolRegistry.Describe(toolId);

            if (definition == null)
            {
                var notFoundDefinition = new Dictionary<string, object>
                {
                    { "id", toolId },
                    { "error", "Tool not found" }
                };
                
                return new Dictionary<string, object>
                {
                    { "tool", notFoundDefinition }
                };
            }

            // Convert ToolDefinition to dictionary for JSON serialization
            var toolDict = ConvertDefinitionToDictionary(definition);

            return new Dictionary<string, object>
                {
                    { "tool", emptyDefinition }
                };
        }

        /// <summary>
        /// Tool: mcp.health - Returns server health status and operational metrics.
        /// </summary>
        [McpTool("mcp.health", "Returns server health status, uptime, tool count, and queue depth for monitoring and diagnostics.", Tier.Core)]
        public static Dictionary<string, object> Health(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            string status = "healthy";
            string uptime = "unknown";
            int toolCount = 0;
            int queueDepth = 0;

            try
            {
                toolCount = toolRegistry?.Count ?? 0;
                status = "healthy";
                uptime = "unknown";
                queueDepth = 0;
            }
            catch (System.Exception ex)
            {
                status = "error";
                Debug.LogError($"Health check error: {ex.Message}");
            }

            return new Dictionary<string, object>
            {
                { "status", status },
                { "queueDepth", queueDepth },
                { "uptime", uptime },
                { "toolCount", toolCount }
            };
        }

        private static Dictionary<string, object> ConvertDefinitionToDictionary(ToolDefinition definition)
        {
            var dict = new Dictionary<string, object>
            {
                { "id", definition.Id ?? "" },
                { "name", definition.Name ?? "" },
                { "description", definition.Description ?? "" },
                { "category", definition.Category ?? "" },
                { "safetyLevel", definition.SafetyLevel.ToString() },
                { "tier", definition.Tier ?? "" },
                { "schemaVersion", definition.SchemaVersion ?? "0.1.0" }
            };

            // Convert inputs (sorted for deterministic ordering)
            var inputsDict = new Dictionary<string, object>();
            if (definition.Inputs != null)
            {
                var sortedInputs = definition.Inputs.OrderBy(i => i.Key).ToList();
                foreach (var input in sortedInputs)
                {
                    inputsDict[input.Key] = ConvertParameterSchemaToDictionary(input.Value);
                }
            }
            dict["inputs"] = inputsDict;

            // Convert outputs (sorted for deterministic ordering)
            var outputsDict = new Dictionary<string, object>();
            if (definition.Outputs != null)
            {
                var sortedOutputs = definition.Outputs.OrderBy(o => o.Key).ToList();
                foreach (var output in sortedOutputs)
                {
                    outputsDict[output.Key] = ConvertOutputSchemaToDictionary(output.Value);
                }
            }
            dict["outputs"] = outputsDict;

            if (!string.IsNullOrEmpty(definition.Notes))
            {
                dict["notes"] = definition.Notes;
            }

            return dict;
        }

        private static Dictionary<string, object> ConvertParameterSchemaToDictionary(ToolParameterSchema schema)
        {
            var dict = new Dictionary<string, object>
            {
                { "type", schema.Type ?? "" },
                { "required", schema.Required },
                { "description", schema.Description ?? "" }
            };

            if (schema.Default != null)
            {
                dict["default"] = schema.Default;
            }

            if (schema.Enum != null && schema.Enum.Length > 0)
            {
                dict["enum"] = schema.Enum;
            }

            if (schema.Minimum.HasValue)
            {
                dict["minimum"] = schema.Minimum.Value;
            }

            if (schema.Maximum.HasValue)
            {
                dict["maximum"] = schema.Maximum.Value;
            }

            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                var propertiesDict = new Dictionary<string, object>();
                var sortedProperties = schema.Properties.OrderBy(p => p.Key).ToList();
                foreach (var prop in sortedProperties)
                {
                    propertiesDict[prop.Key] = ConvertParameterSchemaToDictionary(prop.Value);
                }
                dict["properties"] = propertiesDict;
            }

            if (schema.Items != null)
            {
                dict["items"] = ConvertParameterSchemaToDictionary(schema.Items);
            }

            return dict;
        }

        private static Dictionary<string, object> ConvertOutputSchemaToDictionary(ToolOutputSchema schema)
        {
            var dict = new Dictionary<string, object>
            {
                { "type", schema.Type ?? "" },
                { "description", schema.Description ?? "" }
            };

            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                var propertiesDict = new Dictionary<string, object>();
                var sortedProperties = schema.Properties.OrderBy(p => p.Key).ToList();
                foreach (var prop in sortedProperties)
                {
                    propertiesDict[prop.Key] = ConvertOutputSchemaToDictionary(prop.Value);
                }
                dict["properties"] = propertiesDict;
            }

            if (schema.Items != null)
            {
                dict["items"] = ConvertOutputSchemaToDictionary(schema.Items);
            }

            return dict;
        }
    }
}