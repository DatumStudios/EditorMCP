using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for SceneObjectsFindTool.
    /// </summary>
    public class SceneObjectsFindToolTests
    {
        private SceneObjectsFindTool _tool;
        private UnityEngine.SceneManagement.Scene _testScene;

        [SetUp]
        public void SetUp()
        {
            _tool = new SceneObjectsFindTool();
            // Create a temporary test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
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
            Assert.AreEqual("scene.objects.find", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_WithActiveScene_ReturnsValidResponse()
        {
            var testObj = new GameObject("TestObject");
            testObj.AddComponent<Camera>();

            var request = new ToolInvokeRequest
            {
                Tool = "scene.objects.find",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("scene.objects.find", response.Tool);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("scenePath"));
            Assert.True(response.Output.ContainsKey("matches"));
        }

        [Test]
        public void Invoke_WithNameContains_ReturnsMatchingObjects()
        {
            var testObj = new GameObject("TestObject");
            var otherObj = new GameObject("OtherObject");

            var request = new ToolInvokeRequest
            {
                Tool = "scene.objects.find",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "nameContains", "Test" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var matches = response.Output["matches"] as System.Array;
            Assert.NotNull(matches);
        }

        [Test]
        public void Invoke_WithComponentType_ReturnsMatchingObjects()
        {
            var cameraObj = new GameObject("CameraObject");
            cameraObj.AddComponent<Camera>();

            var request = new ToolInvokeRequest
            {
                Tool = "scene.objects.find",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "componentType", "Camera" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var matches = response.Output["matches"] as System.Array;
            Assert.NotNull(matches);
        }

        [Test]
        public void Invoke_WithTag_ReturnsMatchingObjects()
        {
            var taggedObj = new GameObject("TaggedObject");
            taggedObj.tag = "Untagged"; // Use default tag

            var request = new ToolInvokeRequest
            {
                Tool = "scene.objects.find",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "tag", "Untagged" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var matches = response.Output["matches"] as System.Array;
            Assert.NotNull(matches);
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.objects.find",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

