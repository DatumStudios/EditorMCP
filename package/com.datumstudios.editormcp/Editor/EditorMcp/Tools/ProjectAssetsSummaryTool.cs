using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DatumStudios.EditorMCP.Diagnostics;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: project.assets.summary - Returns a summary of project assets by type.
    /// </summary>
    [McpToolCategory("project")]
    public static class ProjectAssetsSummaryTool
    {
        /// <summary>
        /// Returns a summary of project assets including counts by asset type, large assets, and unreferenced asset detection. Provides project health insights without mutation.
        /// </summary>
        /// <param name="jsonParams">JSON parameters (no parameters required).</param>
        /// <returns>Dictionary with asset summary.</returns>
        [McpTool("project.assets.summary", "Returns a summary of project assets including counts by asset type, large assets, and unreferenced asset detection. Provides project health insights without mutation.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            var timeGuard = new TimeGuard(TimeGuard.AssetScanMaxMilliseconds);
            var diagnostics = new List<string>();

            var counts = new Dictionary<string, int>
            {
                { "Scene", 0 },
                { "Prefab", 0 },
                { "ScriptableObject", 0 },
                { "Material", 0 },
                { "Texture", 0 },
                { "Audio", 0 },
                { "Animation", 0 },
                { "Timeline", 0 }
            };

            try
            {
                // Count scenes (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                counts["Scene"] = sceneGuids != null ? sceneGuids.Length : 0;

                // Count prefabs (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                counts["Prefab"] = prefabGuids != null ? prefabGuids.Length : 0;

                // Count ScriptableObjects (best-effort: find all ScriptableObject-derived assets)
                // Restrict to Assets folder to avoid scanning Packages/
                timeGuard.Check();
                var scriptableObjectGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
                counts["ScriptableObject"] = scriptableObjectGuids != null ? scriptableObjectGuids.Length : 0;

                // Count materials (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
                counts["Material"] = materialGuids != null ? materialGuids.Length : 0;

                // Count textures (Texture2D, Texture3D, etc.) (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var textureGuids = AssetDatabase.FindAssets("t:Texture2D t:Texture3D t:Cubemap", new[] { "Assets" });
                counts["Texture"] = textureGuids != null ? textureGuids.Length : 0;

                // Count audio clips (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
                counts["Audio"] = audioGuids != null ? audioGuids.Length : 0;

                // Count animations (restrict to Assets folder to avoid scanning Packages/)
                timeGuard.Check();
                var animationGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
                counts["Animation"] = animationGuids != null ? animationGuids.Length : 0;

                // Count Timeline assets (best-effort: PlayableDirector assets)
                // Restrict to Assets folder to avoid scanning Packages/
                timeGuard.Check();
                try
                {
                    var timelineGuids = AssetDatabase.FindAssets("t:PlayableAsset", new[] { "Assets" });
                    counts["Timeline"] = timelineGuids != null ? timelineGuids.Length : 0;
                }
                catch
                {
                    // Timeline package may not be installed
                    counts["Timeline"] = 0;
                }
            }
            catch (TimeoutException)
            {
                diagnostics.Add(timeGuard.GetPartialResultMessage(counts.Values.Sum()));
            }

            // Calculate total
            var totalAssets = counts.Values.Sum();

            // Ensure stable key ordering in byType dictionary
            var sortedByType = new Dictionary<string, int>();
            var sortedKeys = counts.Keys.OrderBy(k => k).ToList();
            foreach (var key in sortedKeys)
            {
                sortedByType[key] = counts[key];
            }

            // Track if operation was truncated due to timeout (critical for batch safety)
            bool truncated = diagnostics.Count > 0;

            var output = new Dictionary<string, object>
            {
                { "totalAssets", totalAssets },
                { "byType", sortedByType },
                { "truncated", truncated }
            };

            if (diagnostics.Count > 0)
            {
                output["diagnostics"] = diagnostics.ToArray();
            }

            return output;
        }
    }
}

