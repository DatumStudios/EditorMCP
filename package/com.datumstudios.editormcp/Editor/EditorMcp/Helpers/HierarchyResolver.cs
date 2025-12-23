using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DatumStudios.EditorMCP.Helpers
{
    /// <summary>
    /// Helper class for resolving GameObject hierarchy paths and operations.
    /// Shared infrastructure used by Core, Pro, Studio, and Enterprise tools.
    /// </summary>
    public static class HierarchyResolver
    {
        /// <summary>
        /// Finds a GameObject by its hierarchy path in the active scene.
        /// Uses Transform.Find() for efficient path resolution.
        /// </summary>
        /// <param name="path">Hierarchy path (e.g., "Root/Enemies/Goblin")</param>
        /// <returns>GameObject if found, null otherwise</returns>
        public static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded)
                return null;

            var pathParts = path.Split('/');
            if (pathParts.Length == 0)
                return null;

            // Find root GameObject
            var rootObjects = scene.GetRootGameObjects();
            var rootGo = rootObjects.FirstOrDefault(go => go.name == pathParts[0]);
            if (rootGo == null)
                return null;

            // If only root, return it
            if (pathParts.Length == 1)
                return rootGo;

            // Navigate down the hierarchy using Transform.Find()
            Transform current = rootGo.transform;
            for (int i = 1; i < pathParts.Length; i++)
            {
                var child = current.Find(pathParts[i]);
                if (child == null)
                {
                    // Try direct child search as fallback
                    bool found = false;
                    foreach (Transform c in current)
                    {
                        if (c.name == pathParts[i])
                        {
                            current = c;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return null;
                }
                else
                {
                    current = child;
                }
            }

            return current.gameObject;
        }

        /// <summary>
        /// Extension method to get the full hierarchy path of a Transform.
        /// </summary>
        /// <param name="transform">The Transform to get the path for</param>
        /// <returns>Full hierarchy path (e.g., "Root/Enemies/Goblin")</returns>
        public static string FullHierarchyPath(this Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var path = new List<string>();
            var current = transform;

            while (current != null)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }

        /// <summary>
        /// Gets all GameObjects in the scene hierarchy (recursive).
        /// </summary>
        /// <param name="scene">The scene to traverse</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects</param>
        /// <returns>All GameObjects in the scene</returns>
        public static IEnumerable<GameObject> GetAllGameObjects(Scene scene, bool includeInactive = false)
        {
            if (!scene.isLoaded)
                yield break;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                foreach (var obj in GetAllGameObjectsRecursive(rootObj, includeInactive))
                {
                    yield return obj;
                }
            }
        }

        private static IEnumerable<GameObject> GetAllGameObjectsRecursive(GameObject root, bool includeInactive)
        {
            if (includeInactive || root.activeSelf)
            {
                yield return root;
            }

            foreach (Transform child in root.transform)
            {
                foreach (var obj in GetAllGameObjectsRecursive(child.gameObject, includeInactive))
                {
                    yield return obj;
                }
            }
        }
    }
}

