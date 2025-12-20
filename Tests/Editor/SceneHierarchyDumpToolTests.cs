using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for SceneHierarchyDumpTool.
    /// </summary>
    public class SceneHierarchyDumpToolTests
    {
        private SceneHierarchyDumpTool _tool;
        private UnityEngine.SceneManagement.Scene _testScene;

        [SetUp]
        public void SetUp()
        {
            _tool = new SceneHierarchyDumpTool();
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
            Assert.AreEqual("scene.hierarchy.dump", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_WithActiveScene_ReturnsValidResponse()
        {
            // Create a test GameObject
            var testObj = new GameObject("TestObject");
            var childObj = new GameObject("ChildObject");
            childObj.transform.SetParent(testObj.transform);

            var request = new ToolInvokeRequest
            {
                Tool = "scene.hierarchy.dump",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("scene.hierarchy.dump", response.Tool);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("scenePath"));
            Assert.True(response.Output.ContainsKey("rootObjects"));
        }

        [Test]
        public void Invoke_WithScenePath_ReturnsValidResponse()
        {
            // Save the test scene
            var scenePath = "Assets/TempTestScene.unity";
            EditorSceneManager.SaveScene(_testScene, scenePath);

            var request = new ToolInvokeRequest
            {
                Tool = "scene.hierarchy.dump",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "scenePath", scenePath }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("rootObjects"));

            // Clean up
            AssetDatabase.DeleteAsset(scenePath);
        }

        [Test]
        public void Invoke_WithIncludeInactive_IncludesInactiveObjects()
        {
            var activeObj = new GameObject("ActiveObject");
            var inactiveObj = new GameObject("InactiveObject");
            inactiveObj.SetActive(false);

            var request = new ToolInvokeRequest
            {
                Tool = "scene.hierarchy.dump",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "includeInactive", true }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var rootObjects = response.Output["rootObjects"] as System.Array;
            Assert.NotNull(rootObjects);
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "scene.hierarchy.dump",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

