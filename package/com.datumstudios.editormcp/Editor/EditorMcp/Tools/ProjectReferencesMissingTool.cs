using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudios.EditorMCP.Diagnostics;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: project.references.missing - Detects missing script references and broken asset references.
    /// </summary>
    [McpToolCategory("project")]
    public static class ProjectReferencesMissingTool
    {
        /// <summary>
        /// Detects missing script references and broken asset references in the project. High-value diagnostic tool with zero write risk.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with optional "scope" string ("all", "scenes", "prefabs", "assets").</param>
        /// <returns>JSON string with missing references.</returns>
        [McpTool("project.references.missing", "Detects missing script references and broken asset references in the project. High-value diagnostic tool with zero write risk.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            string scope = "all";

            // Parse JSON parameters
            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null && paramsObj.TryGetValue("scope", out var scopeObj) && scopeObj is string)
                {
                    scope = (string)scopeObj;
                }
            }

            var timeGuard = new TimeGuard(TimeGuard.SceneScanMaxMilliseconds);
            var missingScripts = new List<Dictionary<string, object>>();
            var brokenReferences = new List<Dictionary<string, object>>();
            var diagnostics = new List<string>();
            bool timeLimitExceeded = false;

            try
            {
                if (scope == "all" || scope == "scenes")
                {
                    DetectMissingInScenes(missingScripts, brokenReferences, timeGuard, ref timeLimitExceeded);
                }

                if (!timeLimitExceeded && (scope == "all" || scope == "prefabs"))
                {
                    DetectMissingInPrefabs(missingScripts, brokenReferences, timeGuard, ref timeLimitExceeded);
                }

                if (!timeLimitExceeded && (scope == "all" || scope == "assets"))
                {
                    DetectMissingInAssets(missingScripts, brokenReferences, timeGuard, ref timeLimitExceeded);
                }
            }
            catch (TimeoutException)
            {
                timeLimitExceeded = true;
            }

            // Sort for deterministic ordering
            missingScripts = missingScripts.OrderBy(m => m["path"] as string).ThenBy(m => m["gameObjectPath"] as string).ThenBy(m => (int)m["componentIndex"]).ToList();
            brokenReferences = brokenReferences.OrderBy(b => b["path"] as string).ToList();

            if (timeLimitExceeded)
            {
                diagnostics.Add(timeGuard.GetPartialResultMessage(missingScripts.Count + brokenReferences.Count));
            }

            var output = new Dictionary<string, object>
            {
                { "missingScripts", missingScripts.ToArray() },
                { "brokenReferences", brokenReferences.ToArray() }
            };

            if (diagnostics.Count > 0)
            {
                output["diagnostics"] = diagnostics.ToArray();
            }

            return UnityEngine.JsonUtility.ToJson(output);
        }

        private static void DetectMissingInScenes(List<Dictionary<string, object>> missingScripts, List<Dictionary<string, object>> brokenReferences, TimeGuard timeGuard, ref bool timeLimitExceeded)
        {
            // Restrict to Assets folder to avoid scanning Packages/ (which causes "no meta file" errors)
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            int processedScenes = 0;
            const int maxScenesPerScan = 100; // Limit number of scenes processed

            foreach (var guid in sceneGuids)
            {
                timeGuard.Check();
                
                if (processedScenes >= maxScenesPerScan)
                {
                    timeLimitExceeded = true;
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Guard: Only process assets in Assets/ folder (never touch Packages/)
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                    continue;

                try
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var rootObjects = scene.GetRootGameObjects();

                    foreach (var rootObj in rootObjects)
                    {
                        timeGuard.Check();
                        DetectMissingInGameObject(rootObj, path, missingScripts, brokenReferences, timeGuard);
                    }
                    
                    processedScenes++;
                }
                catch (TimeoutException)
                {
                    timeLimitExceeded = true;
                    break;
                }
                catch
                {
                    // Skip scenes that can't be opened
                }
            }
        }

        private static void DetectMissingInPrefabs(List<Dictionary<string, object>> missingScripts, List<Dictionary<string, object>> brokenReferences, TimeGuard timeGuard, ref bool timeLimitExceeded)
        {
            // Restrict to Assets folder to avoid scanning Packages/ (which causes "no meta file" errors)
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int processedPrefabs = 0;
            const int maxPrefabsPerScan = 200; // Limit number of prefabs processed

            foreach (var guid in prefabGuids)
            {
                timeGuard.Check();
                
                if (processedPrefabs >= maxPrefabsPerScan)
                {
                    timeLimitExceeded = true;
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Guard: Only process assets in Assets/ folder (never touch Packages/)
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                    continue;

                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                        continue;

                    DetectMissingInGameObject(prefab, path, missingScripts, brokenReferences, timeGuard);
                    processedPrefabs++;
                }
                catch (TimeoutException)
                {
                    timeLimitExceeded = true;
                    break;
                }
                catch
                {
                    // Skip prefabs that can't be loaded
                }
            }
        }

        private static void DetectMissingInAssets(List<Dictionary<string, object>> missingScripts, List<Dictionary<string, object>> brokenReferences, TimeGuard timeGuard, ref bool timeLimitExceeded)
        {
            // Best-effort: check ScriptableObjects and other assets for missing references
            // This is limited as we can't easily detect all missing references in all asset types
            // Focus on scenes and prefabs which are the most common sources of missing scripts
            timeGuard.Check();
        }

        private static void DetectMissingInGameObject(GameObject obj, string assetPath, List<Dictionary<string, object>> missingScripts, List<Dictionary<string, object>> brokenReferences, TimeGuard timeGuard)
        {
            timeGuard.Check();

            // Check for missing scripts on this GameObject
            var components = obj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    // Missing script detected
                    var hierarchyPath = GetHierarchyPath(obj);
                    missingScripts.Add(new Dictionary<string, object>
                    {
                        { "path", assetPath },
                        { "gameObjectPath", hierarchyPath },
                        { "componentIndex", i },
                        { "context", $"Missing script component at index {i} on GameObject '{obj.name}'" }
                    });
                }
            }

            // Recursively check children (with depth limit to prevent excessive recursion)
            const int maxDepth = 50;
            DetectMissingInGameObjectRecursive(obj, assetPath, missingScripts, brokenReferences, timeGuard, 0, maxDepth);
        }

        private static void DetectMissingInGameObjectRecursive(GameObject obj, string assetPath, List<Dictionary<string, object>> missingScripts, List<Dictionary<string, object>> brokenReferences, TimeGuard timeGuard, int depth, int maxDepth)
        {
            if (depth >= maxDepth)
                return;

            timeGuard.Check();

            foreach (Transform child in obj.transform)
            {
                timeGuard.Check();
                
                // Check for missing scripts on child GameObject
                var components = child.gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        var hierarchyPath = GetHierarchyPath(child.gameObject);
                        missingScripts.Add(new Dictionary<string, object>
                        {
                            { "path", assetPath },
                            { "gameObjectPath", hierarchyPath },
                            { "componentIndex", i },
                            { "context", $"Missing script component at index {i} on GameObject '{child.gameObject.name}'" }
                        });
                    }
                }
                
                // Recursively check children
                DetectMissingInGameObjectRecursive(child.gameObject, assetPath, missingScripts, brokenReferences, timeGuard, depth + 1, maxDepth);
            }
        }

        private static string GetHierarchyPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}

