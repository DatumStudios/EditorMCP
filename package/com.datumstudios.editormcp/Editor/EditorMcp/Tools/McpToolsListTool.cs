using System.Collections.Generic;
using System.Linq;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: mcp.tools.list - Lists all available tools with their metadata.
    /// </summary>
    [McpToolCategory("mcp.platform")]
    public static class McpToolsListTool
    {
        /// <summary>
        /// Lists all available tools with their metadata, categories, and tier availability. Required for MCP client discovery.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with optional "category" and "tier" filters.</param>
        /// <returns>JSON string with list of tool summaries.</returns>
        [McpTool("mcp.tools.list", "Lists all available tools with their metadata, categories, and tier availability. Required for MCP client discovery.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            if (toolRegistry == null)
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "tools", new object[0] },
                    { "error", "ToolRegistry not initialized" }
                });
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

            var result = new Dictionary<string, object>
            {
                { "tools", toolSummaries }
            };

            return UnityEngine.JsonUtility.ToJson(result);
        }
    }
}

