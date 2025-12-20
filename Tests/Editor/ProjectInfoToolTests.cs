using NUnit.Framework;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for ProjectInfoTool.
    /// </summary>
    public class ProjectInfoToolTests
    {
        private ProjectInfoTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectInfoTool();
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("project.info", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("project.info", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsRequiredFields()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("productName"));
            Assert.True(response.Output.ContainsKey("unityVersion"));
            Assert.True(response.Output.ContainsKey("platform"));
            Assert.True(response.Output.ContainsKey("activeBuildTarget"));
            Assert.True(response.Output.ContainsKey("renderPipeline"));
        }

        [Test]
        public void Invoke_ReturnsNonEmptyValues()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.IsNotEmpty(response.Output["unityVersion"] as string);
            Assert.IsNotEmpty(response.Output["platform"] as string);
            Assert.IsNotEmpty(response.Output["activeBuildTarget"] as string);
            Assert.IsNotEmpty(response.Output["renderPipeline"] as string);
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.info",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

