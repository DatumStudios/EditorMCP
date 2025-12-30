using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: project.scenes.list - Lists all scenes in the project.
    /// </summary>
    [McpToolCategory("project")]
    public static class ProjectScenesListTool
    {
        /// <summary>
        /// Lists all scenes in the project with their paths and build settings status. Provides foundational context for scene-based operations.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with optional "includeAllScenes" boolean.</param>
        /// <returns>Dictionary with list of scenes.</returns>
        [McpTool("project.scenes.list", "Lists all scenes in project with their paths and build settings status. Provides foundational context for scene-based operations.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            bool includeAllScenes = false;

            // Parse JSON parameters
            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null && paramsObj.TryGetValue("includeAllScenes", out var includeAllObj))
                {
                    if (includeAllObj is bool)
                    {
                        includeAllScenes = (bool)includeAllObj;
                    }
                }
            }

            var scenes = new List<Dictionary<string, object>>();

            // Always include Build Settings scenes
            var buildScenes = EditorBuildSettings.scenes;
            var buildScenePaths = new HashSet<string>();

            foreach (var buildScene in buildScenes)
            {
                if (string.IsNullOrEmpty(buildScene.path))
                    continue;

                buildScenePaths.Add(buildScene.path);
                scenes.Add(new Dictionary<string, object>
                {
                    { "path", buildScene.path },
                    { "name", System.IO.Path.GetFileNameWithoutExtension(buildScene.path) },
                    { "enabledInBuild", buildScene.enabled },
                    { "buildIndex", Array.IndexOf(buildScenes, buildScene) }
                });
            }

            // Optionally include all scenes in Assets (restrict to Assets folder to avoid scanning Packages/)
            if (includeAllScenes)
            {
                var allSceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                foreach (var guid in allSceneGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    // Guard: Only process assets in Assets/ folder (never touch Packages/)
                    if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                        continue;

                    // Skip if already in build settings
                    if (buildScenePaths.Contains(path))
                        continue;

                    scenes.Add(new Dictionary<string, object>
                    {
                        { "path", path },
                        { "name", System.IO.Path.GetFileNameWithoutExtension(path) },
                        { "enabledInBuild", false },
                        { "buildIndex", -1 }
                    });
                }
            }

            // Sort by path for stable ordering
            scenes = scenes.OrderBy(s => s["path"] as string).ToList();

            return new Dictionary<string, object>
            {
                { "scenes", scenes }
            };
        }
    }
}

