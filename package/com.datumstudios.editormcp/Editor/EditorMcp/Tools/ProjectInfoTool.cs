using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: project.info - Returns high-level project information.
    /// </summary>
    [McpToolCategory("project")]
    public static class ProjectInfoTool
    {
        /// <summary>
        /// Returns high-level project information including Unity version, render pipeline, build targets, and project configuration.
        /// </summary>
        /// <param name="jsonParams">JSON parameters (no parameters required).</param>
        /// <returns>Dictionary with project information.</returns>
        [McpTool("project.info", "Returns high-level project information including Unity version, render pipeline, build targets, and project configuration. Provides foundational context for all other operations.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            var productName = PlayerSettings.productName;
            var unityVersion = Application.unityVersion;
            var platform = Application.platform.ToString();
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            var renderPipeline = DetectRenderPipeline();

            var result = new Dictionary<string, object>
            {
                { "productName", productName },
                { "unityVersion", unityVersion },
                { "platform", platform },
                { "activeBuildTarget", activeBuildTarget },
                { "renderPipeline", renderPipeline }
            };

            return result;
        }

        private static string DetectRenderPipeline()
        {
            var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            if (renderPipelineAsset == null)
            {
                return "Built-in";
            }

            var assetType = renderPipelineAsset.GetType().Name;
            if (assetType.Contains("Universal") || assetType.Contains("URP"))
            {
                return "URP";
            }
            if (assetType.Contains("HighDefinition") || assetType.Contains("HDRP"))
            {
                return "HDRP";
            }

            return "Unknown";
        }
    }
}

