using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: scene.hierarchy.dump - Returns the complete GameObject hierarchy for a scene.
    /// </summary>
    [McpToolCategory("scene")]
    public static class SceneHierarchyDumpTool
    {
        /// <summary>
        /// Returns the complete GameObject hierarchy for a scene, including components per node and object paths. Cornerstone tool for editor reasoning and structural analysis.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with optional "scenePath" string and "includeInactive" boolean.</param>
        /// <returns>JSON string with scene hierarchy.</returns>
        [McpTool("scene.hierarchy.dump", "Returns the complete GameObject hierarchy for a scene, including components per node and object paths. Cornerstone tool for editor reasoning and structural analysis.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            string scenePath = null;
            bool includeInactive = false;

            // Parse JSON parameters
            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null)
                {
                    if (paramsObj.TryGetValue("scenePath", out var scenePathObj) && scenePathObj is string)
                    {
                        scenePath = (string)scenePathObj;
                    }

                    if (paramsObj.TryGetValue("includeInactive", out var includeInactiveObj))
                    {
                        if (includeInactiveObj is bool)
                        {
                            includeInactive = (bool)includeInactiveObj;
                        }
                    }
                }
            }

            UnityEngine.SceneManagement.Scene scene;
            bool sceneWasOpen = false;

            if (string.IsNullOrEmpty(scenePath))
            {
                // Use currently active scene
                scene = EditorSceneManager.GetActiveScene();
                scenePath = scene.path;
            }
            else
            {
                // Check if scene is already open
                scene = EditorSceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    sceneWasOpen = true;
                }
                else
                {
                    // Open specified scene (additively to preserve current scene)
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }

            var rootObjects = new List<Dictionary<string, object>>();
            var rootGameObjects = scene.GetRootGameObjects();

            // Sort for deterministic ordering
            var sortedRoots = rootGameObjects.OrderBy(go => go.name).ToList();

            foreach (var rootObj in sortedRoots)
            {
                if (includeInactive || rootObj.activeSelf)
                {
                    var node = SerializeGameObject(rootObj, includeInactive);
                    rootObjects.Add(node);
                }
            }

            // Clean up if we opened a scene
            if (!string.IsNullOrEmpty(scenePath) && !sceneWasOpen && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, false);
            }

            var result = new Dictionary<string, object>
            {
                { "scenePath", scenePath ?? "" },
                { "rootObjects", rootObjects.ToArray() }
            };

            return UnityEngine.JsonUtility.ToJson(result);
        }

        private static Dictionary<string, object> SerializeGameObject(GameObject obj, bool includeInactive)
        {
            var components = new List<string>();
            var componentInstanceIds = new List<int>();

            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                {
                    components.Add("Missing Script");
                    componentInstanceIds.Add(0);
                }
                else
                {
                    components.Add(component.GetType().Name);
                    componentInstanceIds.Add(component.GetInstanceID());
                }
            }

            var node = new Dictionary<string, object>
            {
                { "name", obj.name },
                { "path", GetHierarchyPath(obj) },
                { "active", obj.activeSelf },
                { "components", components.ToArray() },
                { "componentInstanceIds", componentInstanceIds.ToArray() }
            };

            // Recursively serialize children
            var children = new List<Dictionary<string, object>>();
            var childTransforms = new List<Transform>();
            foreach (Transform child in obj.transform)
            {
                childTransforms.Add(child);
            }

            // Sort children for deterministic ordering
            childTransforms = childTransforms.OrderBy(t => t.name).ToList();

            foreach (var childTransform in childTransforms)
            {
                if (includeInactive || childTransform.gameObject.activeSelf)
                {
                    var childNode = SerializeGameObject(childTransform.gameObject, includeInactive);
                    children.Add(childNode);
                }
            }

            if (children.Count > 0)
            {
                node["children"] = children.ToArray();
            }

            return node;
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

