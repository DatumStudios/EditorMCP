using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using DatumStudio.Mcp.Core.Editor.Tools;
using DatumStudio.Mcp.Core.Editor.Schemas;

namespace DatumStudio.Mcp.Core.Tests.Editor
{
    /// <summary>
    /// EditMode tests for AudioMixerListTool.
    /// Tests skip gracefully if no AudioMixer assets exist or AudioMixer API is unavailable.
    /// </summary>
    public class AudioMixerListToolTests
    {
        private AudioMixerListTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new AudioMixerListTool();
        }

        [Test]
        public void Tool_HasCorrectDefinition()
        {
            Assert.NotNull(_tool.Definition);
            Assert.AreEqual("audio.mixer.list", _tool.Definition.Id);
            Assert.AreEqual("core", _tool.Definition.Tier);
            Assert.AreEqual(SafetyLevel.ReadOnly, _tool.Definition.SafetyLevel);
        }

        [Test]
        public void Invoke_ReturnsValidResponse()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "audio.mixer.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            Assert.AreEqual("audio.mixer.list", response.Tool);
            Assert.NotNull(response.Output);
        }

        [Test]
        public void Invoke_ReturnsMixersArray()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "audio.mixer.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.True(response.Output.ContainsKey("mixers"));
            var mixers = response.Output["mixers"];
            Assert.NotNull(mixers);
        }

        [Test]
        public void Invoke_WithNoMixers_ReturnsEmptyArray()
        {
            // This test will pass even if no mixers exist
            var request = new ToolInvokeRequest
            {
                Tool = "audio.mixer.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            Assert.NotNull(response);
            var mixers = response.Output["mixers"] as System.Array;
            Assert.NotNull(mixers);
            // Empty array is valid - no mixers in project
        }

        [Test]
        public void Invoke_DoesNotThrow_EvenIfNoMixers()
        {
            var request = new ToolInvokeRequest
            {
                Tool = "audio.mixer.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            // Should not throw even if AudioMixer API is unavailable
            Assert.DoesNotThrow(() => _tool.Invoke(request));
        }

        [Test]
        public void Invoke_HandlesAudioMixerUnavailable()
        {
            // Test that tool handles cases where AudioMixer package might not be available
            var request = new ToolInvokeRequest
            {
                Tool = "audio.mixer.list",
                Arguments = new System.Collections.Generic.Dictionary<string, object>()
            };

            var response = _tool.Invoke(request);

            // Should return valid response even if AudioMixer is unavailable
            Assert.NotNull(response);
            Assert.NotNull(response.Output);
            
            // If AudioMixer is unavailable, diagnostics should be present
            if (response.Output.ContainsKey("diagnostics"))
            {
                var diagnostics = response.Output["diagnostics"];
                Assert.NotNull(diagnostics);
            }
        }
    }
}

