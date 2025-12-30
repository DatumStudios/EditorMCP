using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using DatumStudios.EditorMCP.Schemas;

namespace DatumStudios.EditorMCP.Transport
{
    /// <summary>
    /// Handles stdio-based transport for MCP communication. Reads line-delimited JSON
    /// from stdin and writes responses to stdout. Runs in a background thread to avoid
    /// blocking Unity Editor main thread.
    /// </summary>
    public class StdioTransport : IDisposable
    {
        private readonly McpMessageRouter _router;
        private readonly LineJsonReader _reader;
        private readonly LineJsonWriter _writer;
        private Thread _readThread;
        private bool _isRunning;
        private bool _disposed;

        private DateTime? _lastRequestTime;
        private int _timeoutSeconds;
        private const int DefaultTimeoutSeconds = 30;

        // Throttling configuration
        private readonly Queue<DateTime> _recentRequests;
        private const int MaxRequestsPerWindow = 100;
        private const int ThrottleWindowSeconds = 5;
        private const int ThrottleErrorCode = -32003;

        // Metrics tracking
        private long _messagesReceived;
        private long _messagesSent;
        private long _bytesReceived;
        private long _bytesSent;
        private readonly List<double> _latencies;
        private DateTime _startTime;

        /// <summary>
        /// Initializes a new instance of StdioTransport class.
        /// </summary>
        /// <param name="router">The message router to handle requests.</param>
        /// <param name="timeoutSeconds">Request timeout in seconds (default: 30).</param>
        public StdioTransport(McpMessageRouter router, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _timeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds;
            _recentRequests = new Queue<DateTime>(MaxRequestsPerWindow);
            _latencies = new List<double>();

            _reader = new LineJsonReader(Console.OpenStandardInput());
            _writer = new LineJsonWriter(Console.OpenStandardOutput());
        }

        /// <summary>
        /// Gets configured timeout in seconds.
        /// </summary>
        public int TimeoutSeconds => _timeoutSeconds;

        /// <summary>
        /// Gets time of last request, or null if no requests received.
        /// </summary>
        public DateTime? LastRequestTime => _lastRequestTime;

        /// <summary>
        /// Gets whether transport has timed out (no requests received for timeout period).
        /// </summary>
        public bool IsTimedOut => _lastRequestTime.HasValue &&
            (DateTime.UtcNow - _lastRequestTime.Value).TotalSeconds > _timeoutSeconds;

        /// <summary>
        /// Gets total number of messages received.
        /// </summary>
        public long MessagesReceived => _messagesReceived;

        /// <summary>
        /// Gets total number of messages sent.
        /// </summary>
        public long MessagesSent => _messagesSent;

        /// <summary>
        /// Gets total bytes received.
        /// </summary>
        public long BytesReceived => _bytesReceived;

        /// <summary>
        /// Gets total bytes sent.
        /// </summary>
        public long BytesSent => _bytesSent;

        /// <summary>
        /// Gets average latency in milliseconds.
        /// </summary>
        public double AverageLatencyMs
        {
            get
            {
                if (_latencies.Count == 0)
                    return 0;
                return _latencies.Average();
            }
        }

        /// <summary>
        /// Gets transport metrics as a dictionary.
        /// </summary>
        public Dictionary<string, object> GetMetrics()
        {
            return new Dictionary<string, object>
            {
                { "messagesReceived", _messagesReceived },
                { "messagesSent", _messagesSent },
                { "bytesReceived", _bytesReceived },
                { "bytesSent", _bytesSent },
                { "averageLatencyMs", AverageLatencyMs },
                { "latencySampleCount", _latencies.Count },
                { "uptimeSeconds", _startTime != default(DateTime) ? (DateTime.UtcNow - _startTime).TotalSeconds : 0 }
            };
        }

        /// <summary>
        /// Starts the transport and begins reading from stdin in a background thread.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (_disposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            _isRunning = true;
            _startTime = DateTime.UtcNow;
            _messagesReceived = 0;
            _messagesSent = 0;
            _bytesReceived = 0;
            _bytesSent = 0;
            _latencies.Clear();

            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "EditorMCP StdioTransport Read Thread"
            };
            _readThread.Start();
        }

        /// <summary>
        /// Stops the transport gracefully with shutdown message.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            _reader?.Dispose();
            _writer?.Dispose();

            if (_readThread != null && _readThread.IsAlive)
            {
                if (!_readThread.Join(TimeSpan.FromSeconds(2)))
                {
                    Debug.LogWarning("StdioTransport read thread did not stop within timeout.");
                }
            }

            _readThread = null;
        }

        /// <summary>
        /// Background thread loop that reads lines from stdin and processes them.
        /// </summary>
        private void ReadLoop()
        {
            try
            {
                while (_isRunning && !_reader.EndOfStream)
                {
                    string line = _reader.ReadNextLine();
                    if (line == null)
                    {
                        break;
                    }

                    _lastRequestTime = DateTime.UtcNow;
                    CheckForTimeout();

                    if (IsRateLimited())
                    {
                        SendRateLimitError(null);
                        continue;
                    }

                    _messagesReceived++;
                    _bytesReceived += Encoding.UTF8.GetByteCount(line);

                    try
                    {
                        var request = McpJsonHelper.ParseRequest(line);
                        if (request == null)
                        {
                            SendParseError(null, "Failed to parse request JSON");
                            continue;
                        }

                        _recentRequests.Enqueue(_lastRequestTime.Value);

                        var requestStart = DateTime.UtcNow;
                        var response = _router.Route(request);
                        var requestEnd = DateTime.UtcNow;
                        var latency = (requestEnd - requestStart).TotalMilliseconds;

                        _latencies.Add(latency);

                        if (!_isRunning)
                            break;

                        string responseJson = McpJsonBuilder.BuildResponse(response);
                        _writer.WriteLine(responseJson);

                        _messagesSent++;
                        _bytesSent += Encoding.UTF8.GetByteCount(responseJson);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ObjectDisposedException && !_isRunning)
                        {
                            break;
                        }

                        SendParseError(null, $"JSON parse error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"StdioTransport read loop error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if transport has timed out and logs warning if so.
        /// </summary>
        private void CheckForTimeout()
        {
            if (IsTimedOut)
            {
                var timeSinceLastRequest = (DateTime.UtcNow - _lastRequestTime.Value).TotalSeconds;
                Debug.LogWarning($"StdioTransport timeout warning: No requests received for {timeSinceLastRequest:F1}s (timeout: {_timeoutSeconds}s)");
            }
        }

        /// <summary>
        /// Checks if transport is being rate limited (too many requests in time window).
        /// </summary>
        private bool IsRateLimited()
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-ThrottleWindowSeconds);

            while (_recentRequests.Count > 0 && _recentRequests.Peek() < windowStart)
            {
                _recentRequests.Dequeue();
            }

            return _recentRequests.Count >= MaxRequestsPerWindow;
        }

        /// <summary>
        /// Sends a rate limit error response.
        /// </summary>
        private void SendRateLimitError(object id)
        {
            try
            {
                var errorResponse = new McpResponse
                {
                    JsonRpc = "2.0",
                    Id = id,
                    Error = new McpError
                    {
                        Code = ThrottleErrorCode,
                        Message = $"Rate limited: Maximum {MaxRequestsPerWindow} requests per {ThrottleWindowSeconds} seconds exceeded",
                        Data = new Dictionary<string, object>
                        {
                            { "maxRequests", MaxRequestsPerWindow },
                            { "windowSeconds", ThrottleWindowSeconds },
                            { "retryAfter", 5 }
                        }
                    }
                };
                string errorJson = McpJsonBuilder.BuildResponse(errorResponse);
                _writer.WriteLine(errorJson);
            }
            catch
            {
                Debug.LogError("Failed to send rate limit error response");
            }
        }

        /// <summary>
        /// Sends a parse error response.
        /// </summary>
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
                Debug.LogError("Failed to send parse error response");
            }
        }

        /// <summary>
        /// Disposes the transport and releases resources.
        /// </summary>
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
