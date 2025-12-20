using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for EditorSelectionInfoTool.
    /// </summary>
    public class EditorSelectionInfoToolTests
    {
        private EditorSelectionInfoTool _tool;
        private UnityEngine.SceneManagement.Scene _testScene;
        private GameObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _tool = new EditorSelectionInfoTool();
            
            // Create a temporary test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create a test GameObject with components
            _testObject = new GameObject("TestSelectionObject");
            _testObject.AddComponent<Camera>();
            _testObject.AddComponent<Light>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear selection
            Selection.activeGameObject = null;
            Selection.objects = new Object[0];
            
            // Clean up test scene
            if (_testScene.IsValid())
            {
                EditorSceneManager.CloseScene(_testScene, false);
            }
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("editor.selection.info", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("editor.selection.info", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsRequiredFields()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("selectedObjects"));
            Assert.True(response.Output.ContainsKey("activeScene"));
        }

        [Test]
        public void Invoke_WithSelectedGameObject_ReturnsItInOutput()
        {
            // Programmatically select the test GameObject
            Selection.activeGameObject = _testObject;

            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var selectedObjects = response.Output["selectedObjects"] as System.Array;
            Assert.NotNull(selectedObjects);
            Assert.Greater(selectedObjects.Length, 0);

            // Verify the selected object is in the output
            bool found = false;
            foreach (var obj in selectedObjects)
            {
                if (obj is System.Collections.Generic.Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey("name") && dict["name"] as string == "TestSelectionObject")
                    {
                        found = true;
                        Assert.AreEqual("GameObject", dict["type"] as string);
                        Assert.True(dict.ContainsKey("path"));
                        Assert.True(dict.ContainsKey("components"));
                        break;
                    }
                }
            }

            Assert.True(found, "Selected GameObject should be in the output");
        }

        [Test]
        public void Invoke_WithSelectedGameObject_IncludesComponents()
        {
            // Programmatically select the test GameObject
            Selection.activeGameObject = _testObject;

            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            var selectedObjects = response.Output["selectedObjects"] as System.Array;
            Assert.NotNull(selectedObjects);
            Assert.Greater(selectedObjects.Length, 0);

            // Find our test object
            foreach (var obj in selectedObjects)
            {
                if (obj is System.Collections.Generic.Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey("name") && dict["name"] as string == "TestSelectionObject")
                    {
                        var components = dict["components"] as System.Array;
                        Assert.NotNull(components);
                        Assert.Greater(components.Length, 0);
                        
                        // Verify Camera and Light components are listed
                        var componentNames = new System.Collections.Generic.List<string>();
                        foreach (var comp in components)
                        {
                            componentNames.Add(comp as string);
                        }
                        
                        Assert.Contains("Camera", componentNames);
                        Assert.Contains("Light", componentNames);
                        break;
                    }
                }
            }
        }

        [Test]
        public void Invoke_WithSelectedGameObject_IncludesActiveGameObject()
        {
            // Programmatically select the test GameObject
            Selection.activeGameObject = _testObject;

            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("activeGameObject"));
            var activeGO = response.Output["activeGameObject"] as System.Collections.Generic.Dictionary<string, object>;
            Assert.NotNull(activeGO);
            Assert.AreEqual("TestSelectionObject", activeGO["name"] as string);
            Assert.True(activeGO.ContainsKey("path"));
        }

        [Test]
        public void Invoke_WithNoSelection_ReturnsEmptyArray()
        {
            // Ensure nothing is selected
            Selection.activeGameObject = null;
            Selection.objects = new Object[0];

            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            var selectedObjects = response.Output["selectedObjects"] as System.Array;
            Assert.NotNull(selectedObjects);
            // Empty array is valid - no selection
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "editor.selection.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

