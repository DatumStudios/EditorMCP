using NUnit.Framework;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for ProjectReferencesMissingTool.
    /// </summary>
    public class ProjectReferencesMissingToolTests
    {
        private ProjectReferencesMissingTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectReferencesMissingTool();
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("project.references.missing", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.references.missing",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("project.references.missing", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsRequiredFields()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.references.missing",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("missingScripts"));
            Assert.True(response.Output.ContainsKey("brokenReferences"));
        }

        [Test]
        public void Invoke_WithScope_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.references.missing",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "scope", "scenes" }
                }
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            Assert.True(response.Output.ContainsKey("missingScripts"));
            Assert.True(response.Output.ContainsKey("brokenReferences"));
        }

        [Test]
        public void Invoke_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.references.missing",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }

        [Test]
        public void Invoke_WithInvalidScope_DoesNotThrow()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "project.references.missing",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "scope", "invalid" }
                }
            };

            // Should not throw, but may not process correctly
            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }
    }
}

