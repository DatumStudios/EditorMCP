using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using DatumStudios.EditorMCP.Schemas;
using DatumStudios.EditorMCP.Server;

namespace DatumStudios.EditorMCP.Transport
{
    /// <summary>
    /// Minimal transport loopback self-test for EditorMCP. Tests the transport layer
    /// using in-memory streams without requiring actual stdio. This is a smoke test,
    /// not a full test harness.
    /// </summary>
    public static class EditorMcpTransportLoopbackTest
    {
        /// <summary>
        /// Runs the transport loopback self-test.
        /// </summary>
        [MenuItem("Window/EditorMCP/Run Self-Test")]
        public static void RunTest()
        {
            try
            {
                Debug.Log("[Loopback Test] Starting transport loopback test...");

                var inputStream = new MemoryStream();
                var outputStream = new MemoryStream();

                var server = new EditorMcpServer();
                server.Start();

                var toolCount = server.ToolRegistry.Count;
                Debug.Log(string.Format("[Loopback Test] Server started with {0} tools", toolCount));

                if (toolCount == 0)
                {
                    Debug.LogError("[Loopback Test] FAILURE: No tools discovered in registry");
                    server.Stop();
                    return;
                }

                var hasServerInfo = server.ToolRegistry.IsRegistered("mcp.server.info");
                Debug.Log(string.Format("[Loopback Test] Tool 'mcp.server.info' registered: {0}", hasServerInfo));

                if (!hasServerInfo)
                {
                    Debug.LogError(string.Format("[Loopback Test] FAILURE: Tool 'mcp.server.info' not found"));
                    server.Stop();
                    return;
                }

                var router = new McpMessageRouter(server.ToolRegistry, server.ServerVersion);

                string requestJson = "{\"jsonrpc\":\"2.0\",\"id\":\"test-001\",\"method\":\"tools/call\",\"params\":{\"tool\":\"mcp.server.info\",\"arguments\":{}}}\n";
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
                inputStream.Write(requestBytes, 0, requestBytes.Length);
                inputStream.Flush();
                inputStream.Position = 0;

                var transport = new LoopbackTransport(router, inputStream, outputStream);
                transport.Start();

                int maxWaitMs = 2000;
                var timeoutSeconds = maxWaitMs / 1000.0;
                var start = EditorApplication.timeSinceStartup;

                void Tick()
                {
                    if (outputStream.Length > 0)
                    {
                        EditorApplication.update -= Tick;

                        try
                        {
                            outputStream.Position = 0;
                            byte[] responseBytes = new byte[outputStream.Length];
                            outputStream.Read(responseBytes, 0, responseBytes.Length);
                            string responseJson = Encoding.UTF8.GetString(responseBytes).Trim();

                            Debug.Log(string.Format("[Loopback Test] Response: {0}", responseJson));

                            bool isValid = ValidateResponse(responseJson);
                            if (isValid)
                            {
                                Debug.Log("[Loopback Test] SUCCESS: Test passed");
                            }
                            else
                            {
                                Debug.LogError("[Loopback Test] FAILURE: Validation failed");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(string.Format("[Loopback Test] FAILURE: Exception reading response: {0}", ex.Message));
                        }
                        finally
                        {
                            Debug.Log("[Loopback Test] Calling transport.Stop()...");
                            transport.Stop();
                            transport.Dispose();
                            server.Stop();
                        }

                        return;
                    }

                    if (EditorApplication.timeSinceStartup - start >= timeoutSeconds)
                    {
                        EditorApplication.update -= Tick;

                        Debug.LogError("[Loopback Test] FAILURE: No response (timeout).");

                        try
                        {
                            transport.Stop();
                            transport.Dispose();
                            server.Stop();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(string.Format("[Loopback Test] Exception during cleanup: {0}", ex.Message));
                        }

                        return;
                    }
                }

                EditorApplication.update += Tick;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[Loopback Test] FAILURE: {0}: {1}", ex.GetType().Name, ex.Message));
            }
        }

        /// <summary>
        /// Validates response JSON contains required fields per mcp.server.info tool output schema:
        /// serverVersion, unityVersion, platform, enabledToolCategories, and tier.
        /// Response structure: { "jsonrpc": "2.0", "id": "...", "result": { "tool": "mcp.server.info", "output": { ... } } }
        /// </summary>
        private static bool ValidateResponse(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson))
                return false;

            try
            {
                var hasJsonRpc = responseJson.Contains("\"jsonrpc\"");
                var hasId = responseJson.Contains("\"id\"");
                var hasResult = responseJson.Contains("\"result\"");

                if (!hasJsonRpc || !hasId || !hasResult)
                {
                    Debug.LogWarning(string.Format("[Loopback Test] Missing JSON-RPC envelope fields. Has jsonrpc: {0}, Has id: {1}, Has result: {2}", hasJsonRpc, hasId, hasResult));
                    return false;
                }

                var hasTool = responseJson.Contains("\"tool\"");
                var hasOutput = responseJson.Contains("\"output\"");
                var hasCorrectTool = responseJson.Contains("mcp.server.info");

                if (!hasTool || !hasOutput || !hasCorrectTool)
                {
                    Debug.LogWarning(string.Format("[Loopback Test] Missing tool structure. Has tool: {0}, Has output: {1}, Has mcp.server.info: {2}", hasTool, hasOutput, hasCorrectTool));
                    return false;
                }

                var hasServerVersion = responseJson.Contains("\"serverVersion\"");
                var hasUnityVersion = responseJson.Contains("\"unityVersion\"");
                var hasPlatform = responseJson.Contains("\"platform\"");
                var hasEnabledToolCategories = responseJson.Contains("\"enabledToolCategories\"");
                var hasTier = responseJson.Contains("\"tier\"");

                var allFieldsPresent = hasServerVersion && hasUnityVersion && hasPlatform && hasEnabledToolCategories && hasTier;

                if (!allFieldsPresent)
                {
                    Debug.LogWarning(string.Format("[Loopback Test] Missing tool output fields. serverVersion: {0}, unityVersion: {1}, platform: {2}, enabledToolCategories: {3}, tier: {4}", hasServerVersion, hasUnityVersion, hasPlatform, hasEnabledToolCategories, hasTier));
                }

                return allFieldsPresent;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[Loopback Test] Validation exception: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Loopback transport that uses in-memory streams instead of stdio.
        /// </summary>
        private class LoopbackTransport : IDisposable
        {
            private readonly McpMessageRouter _router;
            private readonly MemoryStream _inputStream;
            private readonly MemoryStream _outputStream;
            private readonly LineJsonReader _reader;
            private readonly LineJsonWriter _writer;
            private Thread _readThread;
            private bool _isRunning;
            private bool _disposed;

            public LoopbackTransport(McpMessageRouter router, MemoryStream inputStream, MemoryStream outputStream)
            {
                _router = router ?? throw new ArgumentNullException(nameof(router));
                _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
                _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

                _reader = new LineJsonReader(_inputStream);
                _writer = new LineJsonWriter(_outputStream);
            }

            public void Start()
            {
                if (_isRunning)
                    return;

                if (_disposed)
                    throw new ObjectDisposedException(nameof(LoopbackTransport));

                _isRunning = true;
                _readThread = new Thread(ReadLoop)
                {
                    IsBackground = true,
                    Name = "EditorMCP Loopback Transport Test"
                };
                _readThread.Start();
            }

            public void Stop()
            {
                if (!_isRunning)
                    return;

                UnityEngine.Debug.Log("[Loopback Transport] Stop() called, setting _isRunning = false");
                _isRunning = false;

                // CRITICAL: Write to input stream FIRST to unblock StreamReader.ReadLine()
                try
                {
                    _inputStream.WriteByte(0);
                    _inputStream.Flush();
                }
                catch
                {
                    // Ignore errors - stream may already be disposed
                }

                _reader?.Dispose();
                _writer?.Dispose();
                _inputStream?.Dispose();
                _outputStream?.Dispose();

            if (_readThread != null && _readThread.IsAlive)
            {
                if (_readThread.Join(TimeSpan.FromSeconds(2)))
                {
                    UnityEngine.Debug.Log("[Loopback Transport] Read thread stopped successfully");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[Loopback Transport] Read thread did not stop within timeout.");
                }
            }

                _readThread = null;
            }

            private void ReadLoop()
            {
                try
                {
                    while (_isRunning && !_reader.EndOfStream)
                    {
                        string line = _reader.ReadNextLine();
                        if (line == null)
                            break;

                        try
                        {
                            var request = McpJsonHelper.ParseRequest(line);
                            if (request == null)
                            {
                                UnityEngine.Debug.LogError("[Loopback Transport] ParseRequest returned null");
                                SendParseError(null, "Failed to parse request JSON");
                                continue;
                            }

                            UnityEngine.Debug.Log($"[Loopback Transport] Parsed request: method={request.Method}, id={request.Id}");

                            var response = _router.Route(request);
                            if (response == null)
                            {
                                UnityEngine.Debug.LogError("[Loopback Transport] Route returned null for request: " + line);
                                continue;
                            }

                            string responseJson = McpJsonBuilder.BuildResponse(response);

                            if (!_disposed && _writer != null)
                            {
                                var positionBefore = _outputStream.Position;
                                var lengthBefore = _outputStream.Length;

                                _writer.WriteLine(responseJson);
                                _writer.Flush();

                                _outputStream.Flush();

                                var positionAfter = _outputStream.Position;
                                var lengthAfter = _outputStream.Length;

                                UnityEngine.Debug.Log(string.Format("[Loopback Transport] Writing response ({0} chars): {1}...", responseJson.Length, responseJson.Substring(0, Math.Min(100, responseJson.Length))));

                                if (lengthAfter <= lengthBefore)
                                {
                                    UnityEngine.Debug.LogWarning(string.Format("[Loopback Transport] WARNING: Stream length did not increase after write! This suggests StreamWriter is still buffering data."));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is ObjectDisposedException && !_isRunning)
                            {
                                break;
                            }

                            UnityEngine.Debug.LogError(string.Format("[Loopback Transport] Error processing request: {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace));

                            if (!_disposed && _writer != null)
                            {
                                SendParseError(null, string.Format("JSON parse error: {0}", ex.Message));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is ObjectDisposedException && !_isRunning))
                    {
                        UnityEngine.Debug.LogError(string.Format("[Loopback Transport] ReadLoop exception: {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace));
                    }
                }
            }

            private void SendParseError(object id, string message)
            {
                try
                {
                    var errorResponse = new McpResponse
                    {
                        JsonRpc = "2.0",
                        Id = id,
                        Error = new McpError
                        {
                            Code = JsonRpcErrorCodes.ParseError,
                            Message = message,
                            Data = new Dictionary<string, object>()
                        }
                    };
                    string errorJson = McpJsonBuilder.BuildResponse(errorResponse);
                    _writer.WriteLine(errorJson);
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    Stop();
                    _reader?.Dispose();
                    _writer?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
