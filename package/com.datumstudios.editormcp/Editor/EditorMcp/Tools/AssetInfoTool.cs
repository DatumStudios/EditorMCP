using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: asset.info - Returns detailed information about a specific asset.
    /// </summary>
    [McpToolCategory("asset")]
    public static class AssetInfoTool
    {
        /// <summary>
        /// Returns detailed information about a specific asset including type, dependencies, and import settings. Demonstrates asset graph awareness without mutation.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with "assetPath" or "guid" string.</param>
        /// <returns>Dictionary with asset information.</returns>
        [McpTool("asset.info", "Returns detailed information about a specific asset including type, dependencies, and import settings. Demonstrates asset graph awareness without mutation.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            string assetPath = null;
            string guid = null;

            // Parse JSON parameters
            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null)
                {
                    if (paramsObj.TryGetValue("assetPath", out var assetPathObj) && assetPathObj is string)
                    {
                        assetPath = (string)assetPathObj;
                    }

                    if (paramsObj.TryGetValue("guid", out var guidObj) && guidObj is string)
                    {
                        guid = (string)guidObj;
                    }
                }
            }

            // Validate input
            if (string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(guid))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Either 'assetPath' or 'guid' must be provided" }
                };
            }

            // Convert GUID to path if needed
            if (!string.IsNullOrEmpty(guid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Asset with GUID '{guid}' not found" }
                    };
                }
            }

            // Guard: Only process assets in Assets/ folder (never touch Packages/)
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/"))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Asset path must be in Assets/ folder. Package assets are not supported." }
                };
            }

            // Validate path exists
            var pathGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(pathGuid))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Asset at path '{assetPath}' not found" }
                };
            }

            // If GUID was provided, verify it matches
            if (!string.IsNullOrEmpty(guid) && pathGuid != guid)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"GUID '{guid}' does not match asset at path '{assetPath}'" }
                };
            }

            // Get asset information
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            var mainObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            var allDependencies = AssetDatabase.GetDependencies(assetPath, false);
            
            // Filter to only Assets/ dependencies (never touch Packages/)
            int dependencyCount = 0;
            if (allDependencies != null)
            {
                foreach (var dep in allDependencies)
                {
                    if (!string.IsNullOrEmpty(dep) && dep.StartsWith("Assets/") && !dep.EndsWith(".meta"))
                    {
                        dependencyCount++;
                    }
                }
            }
            
            var importer = AssetImporter.GetAtPath(assetPath);

            var output = new Dictionary<string, object>
            {
                { "path", assetPath },
                { "guid", assetGuid },
                { "type", assetType != null ? assetType.Name : "Unknown" },
                { "mainObjectName", mainObject != null ? mainObject.name : "" },
                { "dependencyCount", dependencyCount }
            };

            if (importer != null)
            {
                output["importerType"] = importer.GetType().Name;
            }
            else
            {
                output["importerType"] = null;
            }

            return output;
        }
    }
}

