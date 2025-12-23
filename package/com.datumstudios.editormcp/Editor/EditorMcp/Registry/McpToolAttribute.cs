using System;

namespace DatumStudios.EditorMCP.Registry
{
    /// <summary>
    /// Attribute to mark a static method as an MCP tool.
    /// Tools are automatically discovered and registered at Editor startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class McpToolAttribute : Attribute
    {
        /// <summary>
        /// Gets the tool ID (e.g., "go.find", "project.info").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the tool description for discovery/help.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the minimum license tier required to use this tool.
        /// </summary>
        public Tier MinTier { get; }

        /// <summary>
        /// Initializes a new instance of the McpToolAttribute class.
        /// </summary>
        /// <param name="id">The tool ID (e.g., "go.find", "project.info").</param>
        /// <param name="description">Optional tool description for discovery/help.</param>
        /// <param name="minTier">Minimum license tier required (default: Core).</param>
        public McpToolAttribute(string id, string description = "", Tier minTier = Tier.Core)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tool ID cannot be null or empty.", nameof(id));
            }

            Id = id;
            Description = description ?? string.Empty;
            MinTier = minTier;
        }
    }
}

