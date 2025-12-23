using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: asset.dependencies.graph - Returns the dependency graph for an asset.
    /// </summary>
    [McpToolCategory("asset")]
    public static class AssetDependenciesGraphTool
    {
        /// <summary>
        /// Returns the dependency graph for an asset (what it depends on and what depends on it). Useful for understanding asset relationships and impact analysis.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with "assetPath" or "guid" string, optional "depth" integer (1-10), and optional "direction" string ("dependencies", "dependents", "both").</param>
        /// <returns>JSON string with dependency graph.</returns>
        [McpTool("asset.dependencies.graph", "Returns the dependency graph for an asset (what it depends on and what depends on it). Useful for understanding asset relationships and impact analysis.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            string assetPath = null;
            string guid = null;
            int depth = 1;
            string direction = "both";

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

                    if (paramsObj.TryGetValue("depth", out var depthObj))
                    {
                        if (depthObj is int)
                        {
                            depth = (int)depthObj;
                        }
                        else if (depthObj is long)
                        {
                            depth = (int)(long)depthObj;
                        }
                    }

                    if (paramsObj.TryGetValue("direction", out var directionObj) && directionObj is string)
                    {
                        direction = (string)directionObj;
                    }
                }
            }

            // Validate input
            if (string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(guid))
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "error", "Either 'assetPath' or 'guid' must be provided" }
                });
            }

            // Validate depth
            if (depth < 1)
            {
                depth = 1;
            }
            if (depth > 10)
            {
                depth = 10; // Cap at reasonable depth
            }

            // Convert GUID to path if needed
            if (!string.IsNullOrEmpty(guid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                    {
                        { "error", $"Asset with GUID '{guid}' not found" }
                    });
                }
            }

            // Guard: Only process assets in Assets/ folder (never touch Packages/)
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/"))
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "error", $"Asset path must be in Assets/ folder. Package assets are not supported." }
                });
            }

            // Validate path exists
            var pathGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(pathGuid))
            {
                return UnityEngine.JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "error", $"Asset at path '{assetPath}' not found" }
                });
            }

            // Build dependency graph
            var dependencies = new List<Dictionary<string, object>>();
            var dependents = new List<Dictionary<string, object>>();

            if (direction == "dependencies" || direction == "both")
            {
                dependencies = BuildDependencyList(assetPath, depth, true);
            }

            if (direction == "dependents" || direction == "both")
            {
                dependents = BuildDependentList(assetPath, depth);
            }

            var result = new Dictionary<string, object>
            {
                { "assetPath", assetPath },
                { "dependencies", dependencies.ToArray() },
                { "dependents", dependents.ToArray() }
            };

            return UnityEngine.JsonUtility.ToJson(result);
        }

        private static List<Dictionary<string, object>> BuildDependencyList(string assetPath, int maxDepth, bool recursive)
        {
            var result = new List<Dictionary<string, object>>();
            var visited = new HashSet<string>();
            var queue = new Queue<(string path, int currentDepth)>();

            queue.Enqueue((assetPath, 0));

            while (queue.Count > 0)
            {
                var (currentPath, currentDepth) = queue.Dequeue();

                if (currentDepth >= maxDepth || visited.Contains(currentPath))
                    continue;

                visited.Add(currentPath);

                var deps = AssetDatabase.GetDependencies(currentPath, recursive);
                if (deps != null)
                {
                    // Sort for stable ordering
                    var sortedDeps = deps.OrderBy(d => d).ToList();

                    foreach (var dep in sortedDeps)
                    {
                        // Skip self, meta files, and package paths (never touch Packages/)
                        if (dep == currentPath || dep.EndsWith(".meta") || !dep.StartsWith("Assets/"))
                            continue;

                        var depType = AssetDatabase.GetMainAssetTypeAtPath(dep);
                        result.Add(new Dictionary<string, object>
                        {
                            { "path", dep },
                            { "guid", AssetDatabase.AssetPathToGUID(dep) },
                            { "type", depType != null ? depType.Name : "Unknown" },
                            { "depth", currentDepth + 1 }
                        });

                        if (currentDepth + 1 < maxDepth)
                        {
                            queue.Enqueue((dep, currentDepth + 1));
                        }
                    }
                }
            }

            // Sort by depth, then by path for stable ordering
            return result.OrderBy(d => (int)d["depth"]).ThenBy(d => (string)d["path"]).ToList();
        }

        private static List<Dictionary<string, object>> BuildDependentList(string assetPath, int maxDepth)
        {
            var result = new List<Dictionary<string, object>>();
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var visited = new HashSet<string>();
            var queue = new Queue<(string path, int currentDepth)>();

            // Find all assets that depend on this one
            var allAssets = AssetDatabase.FindAssets("", new[] { "Assets" });
            var dependents = new List<string>();

            foreach (var guid in allAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Skip meta files and package paths (never touch Packages/)
                if (string.IsNullOrEmpty(path) || path.EndsWith(".meta") || !path.StartsWith("Assets/"))
                    continue;

                var deps = AssetDatabase.GetDependencies(path, false);
                if (deps != null && System.Array.IndexOf(deps, assetPath) >= 0)
                {
                    dependents.Add(path);
                }
            }

            // Build graph recursively
            queue.Enqueue((assetPath, 0));
            visited.Add(assetPath);

            foreach (var dependent in dependents.OrderBy(p => p))
            {
                // Guard: Skip package paths (never touch Packages/)
                if (!dependent.StartsWith("Assets/"))
                    continue;

                if (!visited.Contains(dependent))
                {
                    var depType = AssetDatabase.GetMainAssetTypeAtPath(dependent);
                    result.Add(new Dictionary<string, object>
                    {
                        { "path", dependent },
                        { "guid", AssetDatabase.AssetPathToGUID(dependent) },
                        { "type", depType != null ? depType.Name : "Unknown" },
                        { "depth", 1 }
                    });
                    visited.Add(dependent);

                    // For depth > 1, find dependents of dependents
                    if (maxDepth > 1)
                    {
                        var deeperDeps = BuildDependentList(dependent, maxDepth - 1);
                        foreach (var deeperDep in deeperDeps)
                        {
                            var deeperPath = (string)deeperDep["path"];
                            if (!visited.Contains(deeperPath))
                            {
                                deeperDep["depth"] = (int)deeperDep["depth"] + 1;
                                result.Add(deeperDep);
                                visited.Add(deeperPath);
                            }
                        }
                    }
                }
            }

            // Sort by depth, then by path for stable ordering
            return result.OrderBy(d => (int)d["depth"]).ThenBy(d => (string)d["path"]).ToList();
        }
    }
}

