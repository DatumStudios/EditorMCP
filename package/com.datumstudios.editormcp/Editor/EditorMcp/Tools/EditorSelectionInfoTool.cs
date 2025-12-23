using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: editor.selection.info - Returns information about currently selected objects and assets in the Unity Editor.
    /// </summary>
    [McpToolCategory("editor")]
    public static class EditorSelectionInfoTool
    {
        /// <summary>
        /// Returns information about currently selected objects and assets in the Unity Editor. Bridges human and AI workflows by providing current editor context.
        /// </summary>
        /// <param name="jsonParams">JSON parameters (no parameters required).</param>
        /// <returns>JSON string with selection information.</returns>
        [McpTool("editor.selection.info", "Returns information about currently selected objects and assets in the Unity Editor. Bridges human and AI workflows by providing current editor context.", Tier.Core)]
        public static string Invoke(string jsonParams)
        {
            var selectedObjects = new List<Dictionary<string, object>>();
            var selectedGameObjects = Selection.gameObjects;
            var selectedAssets = Selection.objects.Where(obj => !(obj is GameObject)).ToList();

            // Process selected GameObjects
            if (selectedGameObjects != null && selectedGameObjects.Length > 0)
            {
                // Sort for deterministic ordering
                var sortedGameObjects = selectedGameObjects.OrderBy(go => GetHierarchyPath(go)).ToList();

                foreach (var go in sortedGameObjects)
                {
                    var components = new List<string>();
                    foreach (var component in go.GetComponents<Component>())
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

                    selectedObjects.Add(new Dictionary<string, object>
                    {
                        { "name", go.name },
                        { "type", "GameObject" },
                        { "instanceId", go.GetInstanceID() },
                        { "path", GetHierarchyPath(go) },
                        { "components", components.ToArray() }
                    });
                }
            }

            // Process selected assets (non-GameObject objects)
            if (selectedAssets != null && selectedAssets.Count > 0)
            {
                // Sort for deterministic ordering
                var sortedAssets = selectedAssets.OrderBy(obj => obj.name).ToList();

                foreach (var asset in sortedAssets)
                {
                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    // Guard: Only process assets in Assets/ folder (never touch Packages/)
                    // Skip package assets to avoid "no meta file" errors
                    if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/"))
                    {
                        // Include asset info but mark as package asset
                        selectedObjects.Add(new Dictionary<string, object>
                        {
                            { "name", asset.name },
                            { "type", asset.GetType().Name },
                            { "instanceId", asset.GetInstanceID() },
                            { "path", assetPath ?? "" },
                            { "guid", "" },
                            { "note", "Package asset (not in Assets/ folder)" }
                        });
                        continue;
                    }

                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                    selectedObjects.Add(new Dictionary<string, object>
                    {
                        { "name", asset.name },
                        { "type", asset.GetType().Name },
                        { "instanceId", asset.GetInstanceID() },
                        { "path", assetPath ?? "" },
                        { "guid", assetGuid ?? "" }
                    });
                }
            }

            // Get active scene
            string activeScenePath = "";
            try
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    activeScenePath = activeScene.path ?? "";
                }
            }
            catch
            {
                // Best-effort: if we can't get active scene, leave empty
            }

            // Get active GameObject
            Dictionary<string, object> activeGameObject = null;
            var activeGO = Selection.activeGameObject;
            if (activeGO != null)
            {
                activeGameObject = new Dictionary<string, object>
                {
                    { "name", activeGO.name },
                    { "path", GetHierarchyPath(activeGO) }
                };
            }

            var output = new Dictionary<string, object>
            {
                { "selectedObjects", selectedObjects.ToArray() },
                { "activeScene", activeScenePath }
            };

            if (activeGameObject != null)
            {
                output["activeGameObject"] = activeGameObject;
            }

            return UnityEngine.JsonUtility.ToJson(output);
        }

        private static string GetHierarchyPath(GameObject obj)
        {
            if (obj == null)
                return "";

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

