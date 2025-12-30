using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: scene.components.list - Returns all components attached to a specific GameObject.
    /// </summary>
    [McpToolCategory("scene")]
    public static class SceneComponentsListTool
    {
        /// <summary>
        /// Returns all components attached to a specific GameObject, including serialized field names. Enables safe inspection before any potential edits.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with "gameObjectPath" string (required) and optional "scenePath" string.</param>
        /// <returns>Dictionary with components list.</returns>
        [McpTool("scene.components.list", "Returns all components attached to a specific GameObject, including serialized field names. Enables safe inspection before any potential edits.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            string scenePath = null;
            string gameObjectPath = null;

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

                    if (paramsObj.TryGetValue("gameObjectPath", out var gameObjectPathObj) && gameObjectPathObj is string)
                    {
                        gameObjectPath = (string)gameObjectPathObj;
                    }
                }
            }

if (string.IsNullOrEmpty(gameObjectPath))
            {
                return new Dictionary<string, object>
                    {
                        { "error", "gameObjectPath is required" }
                    };
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

            // Find GameObject by path
            GameObject targetObject = null;
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                targetObject = FindGameObjectByPath(rootObj, gameObjectPath);
                if (targetObject != null)
                    break;
            }

            var components = new List<Dictionary<string, object>>();

            if (targetObject != null)
            {
                var allComponents = targetObject.GetComponents<Component>();
                for (int i = 0; i < allComponents.Length; i++)
                {
                    var component = allComponents[i];
                    if (component == null)
                    {
                        components.Add(new Dictionary<string, object>
                        {
                            { "type", "Missing Script" },
                            { "instanceId", 0 },
                            { "index", i }
                        });
                    }
                    else
                    {
                        components.Add(new Dictionary<string, object>
                        {
                            { "type", component.GetType().Name },
                            { "instanceId", component.GetInstanceID() },
                            { "index", i },
                            { "fullTypeName", component.GetType().FullName }
                        });
                    }
                }
            }
            else
            {
                // Clean up if we opened a scene
                if (!string.IsNullOrEmpty(scenePath) && !sceneWasOpen && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, false);
                }

                return new Dictionary<string, object>
                {
                    { "error", $"GameObject with path '{gameObjectPath}' not found in scene" }
                };
            }

            // Clean up if we opened a scene
            if (!string.IsNullOrEmpty(scenePath) && !sceneWasOpen && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, false);
            }

            var result = new Dictionary<string, object>
            {
                { "gameObjectPath", gameObjectPath },
                { "scenePath", scenePath ?? "" },
                { "components", components.ToArray() }
            };

            return result;
        }

        private static GameObject FindGameObjectByPath(GameObject root, string path)
        {
            if (root.name == path)
            {
                return root;
            }

            var pathParts = path.Split('/');
            if (pathParts.Length == 0)
                return null;

            if (pathParts[0] != root.name)
                return null;

            if (pathParts.Length == 1)
                return root;

            // Navigate down the hierarchy
            Transform current = root.transform;
            for (int i = 1; i < pathParts.Length; i++)
            {
                var child = current.Find(pathParts[i]);
                if (child == null)
                {
                    // Try direct child search
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
    }
}

