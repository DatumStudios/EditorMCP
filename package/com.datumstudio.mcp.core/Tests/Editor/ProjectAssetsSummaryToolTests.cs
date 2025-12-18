using NUnit.Framework;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for ProjectAssetsSummaryTool.
    /// </summary>
    public class ProjectAssetsSummaryToolTests
    {
        private ProjectAssetsSummaryTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectAssetsSummaryTool();
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("project.assets.summary", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.assets.summary",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("project.assets.summary", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsRequiredFields()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.assets.summary",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("totalAssets"));
            Assert.True(response.Output.ContainsKey("byType"));
        }

        [Test]
        public void Invoke_ReturnsValidCounts()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.assets.summary",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            var totalAssets = response.Output["totalAssets"];
            Assert.NotNull(totalAssets);
            Assert.IsInstanceOf<int>(totalAssets);
            Assert.GreaterOrEqual((int)totalAssets, 0);

            var byType = response.Output["byType"] as System.Collections.Generic.Dictionary<string, object>;
            Assert.NotNull(byType);
            Assert.True(byType.ContainsKey("Scene"));
            Assert.True(byType.ContainsKey("Prefab"));
            Assert.True(byType.ContainsKey("Material"));
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.assets.summary",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

