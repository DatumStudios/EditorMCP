using NUnit.Framework;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for ProjectScenesListTool.
    /// </summary>
    public class ProjectScenesListToolTests
    {
        private ProjectScenesListTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectScenesListTool();
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("project.scenes.list", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.scenes.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("project.scenes.list", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsScenesArray()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.scenes.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("scenes"));
            var scenes = response.Output["scenes"];
            Assert.NotNull(scenes);
        }

        [Test]
        public void Invoke_WithIncludeAllScenes_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.scenes.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "includeAllScenes", true }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("scenes"));
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.scenes.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

