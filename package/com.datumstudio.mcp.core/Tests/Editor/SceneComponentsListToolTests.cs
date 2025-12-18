using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for SceneComponentsListTool.
    /// </summary>
    public class SceneComponentsListToolTests
    {
        private SceneComponentsListTool _tool;
        private UnityEngine.SceneManagement.Scene _testScene;
        private GameObject _testObject;

        [SetUp]
        public void SetUp()
        {
            _tool = new SceneComponentsListTool();
            // Create a temporary test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create a test GameObject with components
            _testObject = new GameObject("TestObject");
            _testObject.AddComponent<Camera>();
            _testObject.AddComponent<Light>();
        }

        [TearDown]
        public void TearDown()
        {
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
            Assert.AreEqual("scene.components.list", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_WithValidPath_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gameObjectPath", "TestObject" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("scene.components.list", response.Tool);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("gameObjectPath"));
            Assert.True(response.Output.ContainsKey("components"));
        }

        [Test]
        public void Invoke_WithValidPath_ReturnsComponents()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gameObjectPath", "TestObject" }
                }
            };

            var response = _tool.Invoke(request);

            var components = response.Output["components"] as System.Array;
            Assert.NotNull(components);
            Assert.Greater(components.Length, 0);
        }

        [Test]
        public void Invoke_WithInvalidPath_ReturnsError()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gameObjectPath", "NonExistentObject" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.True(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithMissingPath_ReturnsError()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.True(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithNestedPath_ReturnsValidResponse()
        {
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            child.AddComponent<Camera>();

            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gameObjectPath", "Parent/Child" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.False(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.components.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gameObjectPath", "TestObject" }
                }
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

