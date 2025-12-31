using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DatumStudios.EditorMCP.Registry;
using DatumStudios.EditorMCP.Schemas;
using DatumStudios.EditorMCP.Tools;
using DatumStudios.EditorMCP.Transport;
using DatumStudios.EditorMCP.Diagnostics;

namespace DatumStudios.EditorMCP.Server
{
    /// <summary>
    /// Main entry point for the EditorMCP server. Manages server lifecycle, tool registry,
    /// and provides status information. This is an editor-only, deterministic, read-only
    /// implementation for Core v0.1.
    /// </summary>
    public class EditorMcpServer
    {
        private readonly ToolRegistry _toolRegistry;
        private TransportHost _transportHost;
        private bool _isRunning;
        private string _serverVersion = "0.1.0";
        private int _port;
        private const string PORT_PREF_KEY = "EditorMcp.Port";
        private const string WAS_RUNNING_PREF_KEY = "EditorMcp.WasRunning";
        private const string AUTO_RESTART_PREF_KEY = "EditorMcp.AutoRestart";
        private const string MAX_RETRIES_PREF_KEY = "EditorMcp.MaxRetries";
        private const int DEFAULT_PORT = 27182;
        private const int DEFAULT_MAX_RETRIES = 3;

        private static EditorMcpServer _instance;

        /// <summary>
        /// Gets the singleton server instance.
        /// </summary>
        public static EditorMcpServer Instance => _instance;

        /// <summary>
        /// Gets the tool registry instance.
        /// </summary>
        public ToolRegistry ToolRegistry => _toolRegistry;

        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        public bool IsRunning => _isRunning && (_transportHost?.IsRunning ?? false);

        /// <summary>
        /// Gets the server version.
        /// </summary>
        public string ServerVersion => _serverVersion;

        /// <summary>
        /// Gets the transport host instance (if running).
        /// </summary>
        public TransportHost TransportHost => _transportHost;

        /// <summary>
        /// Gets the configured port for transport (for future WebSocket support).
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Initializes a new instance of the EditorMcpServer class.
        /// </summary>
        public EditorMcpServer()
        {
            _toolRegistry = new ToolRegistry();
            ToolRegistry.Current = _toolRegistry; // Set static accessor for static tools
            _isRunning = false;
            _port = GetPortForCurrentProject();
            _instance = this;
        }

        private int GetPortForCurrentProject()
        {
            // Try per-project port first
            string projectNameHash = GetProjectNameHash();
            string perProjectKey = $"{PORT_PREF_KEY}.{projectNameHash}";
            int perProjectPort = EditorPrefs.GetInt(perProjectKey, -1);

            if (perProjectPort >= 27182 && perProjectPort <= 65535)
            {
                return perProjectPort;
            }

            // Fallback to global port
            return EditorPrefs.GetInt(PORT_PREF_KEY, DEFAULT_PORT);
        }

        private string GetProjectNameHash()
        {
            // Hash the product name for a consistent project-specific key
            string productName = Application.productName ?? "UnityProject";
            int hash = productName.GetHashCode();
            return hash.ToString("X8"); // 8-character hex hash
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                // Check if server should auto-restart after domain reload
                bool wasRunning = EditorPrefs.GetBool(WAS_RUNNING_PREF_KEY, false);
                bool autoRestart = EditorPrefs.GetBool(AUTO_RESTART_PREF_KEY, false);

                if (wasRunning && autoRestart)
                {
                    Debug.Log("[EditorMCP] Server was running before domain reload. Auto-restarting...");
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            var server = Instance;
                            if (server != null && !server.IsRunning)
                            {
                                server.Start();
                                Debug.Log("[EditorMCP] Server auto-restarted successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[EditorMCP] Failed to auto-restart server: {ex.Message}");
                        }
                    };
                }
                else if (wasRunning && !autoRestart)
                {
                    Debug.Log("[EditorMCP] Server was running before domain reload. Auto-restart is disabled. Manually restart the server to continue.");
                }
            };
        }

        /// <summary>
        /// Starts the MCP server. Initializes the tool registry and starts the stdio transport.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when server is already running or Unity version is incompatible.</exception>
        public void Start()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            // Check Unity version compatibility - block startup if incompatible
            if (!VersionValidator.IsCompatible())
            {
                var errorMessage = $"[EditorMCP] Server cannot start: Requires Unity {VersionValidator.GetMinimumVersion()}+. " +
                    $"Current version: {Application.unityVersion}. " +
                    $"Please upgrade to Unity 2022.3 LTS or later.";
                
                Debug.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Force tool discovery on every start to ensure tools are available
            _toolRegistry.DiscoverAttributeTools(forceRediscovery: true);

            // Create and start transport host
            _transportHost = new TransportHost(_toolRegistry, _serverVersion);
            _transportHost.Start();

            var registeredTools = _toolRegistry.List();
            Debug.Log($"[EditorMCP] Server Start completed: server instance {GetHashCode()}, discovered {registeredTools.Count} tools");

            _isRunning = true;
            EditorPrefs.SetBool(WAS_RUNNING_PREF_KEY, true);
        }

        /// <summary>
        /// Stops the MCP server. Stops the transport and clears the tool registry.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            // Stop transport first
            _transportHost?.Stop("Server stopped");
            _transportHost?.Dispose();
            _transportHost = null;

            _isRunning = false;
            EditorPrefs.SetBool(WAS_RUNNING_PREF_KEY, false);
        }

        /// <summary>
        /// Gets server status information.
        /// </summary>
        /// <returns>Server status including version, Unity version, platform, and enabled categories.</returns>
        public ServerStatus GetStatus()
        {
            return new ServerStatus
            {
                ServerVersion = _serverVersion,
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                EnabledToolCategories = GetEnabledCategories(),
                Tier = "core",
                IsRunning = IsRunning,
                TransportStartedAt = _transportHost?.StartedAt
            };
        }

        // Note: RegisterCoreTools() removed - all tools are now discovered via [McpTool] attributes

        /// <summary>
        /// Gets the list of enabled tool categories.
        /// </summary>
        /// <returns>Array of category names.</returns>
        private string[] GetEnabledCategories()
        {
            var categories = _toolRegistry.List()
                .Select(t => t.Category)
                .Distinct()
                .ToArray();

            return categories;
        }

        /// <summary>
        /// Configures the port for transport (for future WebSocket support).
        /// </summary>
        /// <param name="port">The port number (27182-65535).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when port is out of valid range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when server is running and port change requires restart.</exception>
        public void ConfigurePort(int port, bool perProject = false)
        {
            if (port < 27182 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 27182 and 65535.");
            }

            if (_isRunning && port != _port)
            {
                // Port change requires restart - warn user
                Debug.LogWarning($"EditorMCP: Port change from {_port} to {port} requires server restart. Use Restart() to apply changes.");
            }

            _port = port;

            if (perProject)
            {
                // Save per-project port
                string projectNameHash = GetProjectNameHash();
                string perProjectKey = $"{PORT_PREF_KEY}.{projectNameHash}";
                EditorPrefs.SetInt(perProjectKey, port);
            }
            else
            {
                // Save global port
                EditorPrefs.SetInt(PORT_PREF_KEY, port);
            }
        }

        /// <summary>
        /// Restarts the server with current configuration (stops, then starts).
        /// </summary>
        public void Restart()
        {
            var wasRunning = _isRunning;
            if (wasRunning)
            {
                Stop();
            }

            // Reload port from EditorPrefs in case it was changed externally
            _port = EditorPrefs.GetInt(PORT_PREF_KEY, DEFAULT_PORT);

            if (wasRunning)
            {
                Start();
            }
        }
    }

    /// <summary>
    /// Server status information.
    /// </summary>
    public class ServerStatus
    {
        /// <summary>
        /// EditorMCP server version.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Unity Editor version.
        /// </summary>
        public string UnityVersion { get; set; }

        /// <summary>
        /// Platform the server is running on.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Enabled tool categories.
        /// </summary>
        public string[] EnabledToolCategories { get; set; }

        /// <summary>
        /// Current tier (always "core" for v0.1).
        /// </summary>
        public string Tier { get; set; }

        /// <summary>
        /// Whether the server is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// When the transport was started (if running).
        /// </summary>
        public DateTime? TransportStartedAt { get; set; }
    }
}

