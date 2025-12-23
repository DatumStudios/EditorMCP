using System.Collections.Generic;
using System.Linq;
using DatumStudios.EditorMCP.Registry;
using DatumStudios.EditorMCP.Schemas;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: mcp.tool.describe - Returns the complete schema for a specific tool.
    /// </summary>
    [McpToolCategory("mcp.platform")]
    public static class McpToolDescribeTool
    {
        /// <summary>
        /// Returns the complete schema for a specific tool, including input parameters, output structure, and safety information. Critical for LLM grounding and human inspection.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with "toolId" string parameter.</param>
        /// <returns>JSON string with complete tool definition.</returns>
        [McpTool("mcp.tool.describe", "Returns the complete schema for a specific tool, including input parameters, output structure, and safety information. Critical for LLM grounding and human inspection.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            if (toolRegistry == null)
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "tool", new Dictionary<string, object> { { "error", "ToolRegistry not initialized" } } }
                });
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
                var emptyDefinition = new Dictionary<string, object>
                {
                    { "id", toolId ?? "" },
                    { "error", "Tool not found" }
                };

                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "tool", emptyDefinition }
                });
            }

            var definition = toolRegistry.Describe(toolId);

            if (definition == null)
            {
                var emptyDefinition = new Dictionary<string, object>
                {
                    { "id", toolId },
                    { "error", "Tool not found" }
                };

                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "tool", emptyDefinition }
                });
            }

            // Convert ToolDefinition to dictionary for JSON serialization
            var toolDict = ConvertDefinitionToDictionary(definition);

            return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
            {
                { "tool", toolDict }
            });
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

