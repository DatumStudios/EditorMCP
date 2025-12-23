using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;
using DatumStudios.EditorMCP.Diagnostics;
using DatumStudios.EditorMCP;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: mcp.server.info - Returns server and environment information.
    /// </summary>
    [McpToolCategory("mcp.platform")]
    public static class McpServerInfoTool
    {
        private const string SERVER_VERSION = "0.1.0";

        /// <summary>
        /// Returns server and environment information to verify the MCP bridge is operational and provide context for tool execution.
        /// </summary>
        /// <param name="jsonParams">JSON parameters (no parameters required).</param>
        /// <returns>JSON string with server information.</returns>
        [McpTool("mcp.server.info", "Returns server and environment information to verify the MCP bridge is operational and provide context for tool execution.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            var toolRegistry = ToolRegistry.Current;
            if (toolRegistry == null)
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", "ToolRegistry not initialized" }
                });
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

            var result = new Dictionary<string, object>
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

            return UnityEngine.JsonUtility.ToJson(result);
        }
    }
}

