using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: scene.objects.find - Finds GameObjects in a scene matching specified criteria.
    /// </summary>
    [McpToolCategory("scene")]
    public static class SceneObjectsFindTool
    {
        /// <summary>
        /// Finds GameObjects in a scene matching specified criteria (component type, name pattern, tag, layer). Composable and extremely useful for targeted inspection.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with optional "scenePath", "nameContains", "tag", "layer", "componentType".</param>
        /// <returns>Dictionary with matching GameObjects.</returns>
        [McpTool("scene.objects.find", "Finds GameObjects in a scene matching specified criteria (component type, name pattern, tag, layer). Composable and extremely useful for targeted inspection.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            string scenePath = null;
            string nameContains = null;
            string tag = null;
            int? layer = null;
            string componentType = null;

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

                    if (paramsObj.TryGetValue("nameContains", out var nameContainsObj) && nameContainsObj is string)
                    {
                        nameContains = (string)nameContainsObj;
                    }

                    if (paramsObj.TryGetValue("tag", out var tagObj) && tagObj is string)
                    {
                        tag = (string)tagObj;
                    }

                    if (paramsObj.TryGetValue("layer", out var layerObj))
                    {
                        if (layerObj is int)
                        {
                            layer = (int)layerObj;
                        }
                        else if (layerObj is long)
                        {
                            layer = (int)(long)layerObj;
                        }
                    }

                    if (paramsObj.TryGetValue("componentType", out var componentTypeObj) && componentTypeObj is string)
                    {
                        componentType = (string)componentTypeObj;
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

            var allObjects = scene.GetRootGameObjects()
                .SelectMany(go => GetAllGameObjects(go))
                .ToList();

            var matches = new List<Dictionary<string, object>>();

            foreach (var obj in allObjects)
            {
                if (MatchesCriteria(obj, nameContains, tag, layer, componentType))
                {
                    matches.Add(SerializeGameObject(obj));
                }
            }

            // Sort for deterministic ordering
            matches = matches.OrderBy(m => m["path"] as string).ToList();

            // Clean up if we opened a scene
            if (!string.IsNullOrEmpty(scenePath) && !sceneWasOpen && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, false);
            }

            var result = new Dictionary<string, object>
            {
                { "scenePath", scenePath ?? "" },
                { "matches", matches.ToArray() }
            };

            return result;
        }

        private static IEnumerable<GameObject> GetAllGameObjects(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var childObj in GetAllGameObjects(child.gameObject))
                {
                    yield return childObj;
                }
            }
        }

        private static bool MatchesCriteria(GameObject obj, string nameContains, string tag, int? layer, string componentType)
        {
            if (!string.IsNullOrEmpty(nameContains))
            {
                if (!obj.name.Contains(nameContains))
                    return false;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!obj.CompareTag(tag))
                    return false;
            }

            if (layer.HasValue)
            {
                if (obj.layer != layer.Value)
                    return false;
            }

            if (!string.IsNullOrEmpty(componentType))
            {
                var hasComponent = false;
                foreach (var component in obj.GetComponents<Component>())
                {
                    if (component != null && component.GetType().Name == componentType)
                    {
                        hasComponent = true;
                        break;
                    }
                }
                if (!hasComponent)
                    return false;
            }

            return true;
        }

        private static Dictionary<string, object> SerializeGameObject(GameObject obj)
        {
            var components = new List<string>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                {
                    components.Add("Missing Script");
                }
                else
                {
                    components.Add(component.GetType().Name);
                }
            }

            return new Dictionary<string, object>
            {
                { "name", obj.name },
                { "path", GetHierarchyPath(obj) },
                { "instanceId", obj.GetInstanceID() },
                { "components", components.ToArray() },
                { "tag", obj.tag },
                { "layer", obj.layer }
            };
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

