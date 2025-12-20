using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for AssetDependenciesGraphTool.
    /// </summary>
    public class AssetDependenciesGraphToolTests
    {
        private AssetDependenciesGraphTool _tool;
        private string _testAssetPath;
        private string _testAssetGuid;

        [SetUp]
        public void SetUp()
        {
            _tool = new AssetDependenciesGraphTool();
            
            // Create a temporary test asset
            var testScriptable = ScriptableObject.CreateInstance<TestScriptableObject>();
            _testAssetPath = "Assets/TempTestAsset.asset";
            AssetDatabase.CreateAsset(testScriptable, _testAssetPath);
            AssetDatabase.SaveAssets();
            _testAssetGuid = AssetDatabase.AssetPathToGUID(_testAssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test asset
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(_testAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(_testAssetPath);
            }
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("asset.dependencies.graph", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_WithAssetPath_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("asset.dependencies.graph", response.Tool);
            Assert.NotNull(response.Output);
            Assert.False(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithAssetPath_ReturnsRequiredFields()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath }
                }
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("assetPath"));
            Assert.True(response.Output.ContainsKey("dependencies"));
            Assert.True(response.Output.ContainsKey("dependents"));
        }

        [Test]
        public void Invoke_WithGuid_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "guid", _testAssetGuid }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.False(response.Output.ContainsKey("error"));
            Assert.AreEqual(_testAssetPath, response.Output["assetPath"]);
        }

        [Test]
        public void Invoke_WithDepth_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath },
                    { "depth", 2 }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.False(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithDirection_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath },
                    { "direction", "dependencies" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.False(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithInvalidPath_ReturnsError()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", "Assets/NonExistentAsset.asset" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.True(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithInvalidGuid_ReturnsError()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "guid", "00000000000000000000000000000000" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.True(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithNoInput_ReturnsError()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.True(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_WithInvalidDepth_ClampsDepth()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath },
                    { "depth", 0 }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.False(response.Output.ContainsKey("error"));
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "asset.dependencies.graph",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "assetPath", _testAssetPath }
                }
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

