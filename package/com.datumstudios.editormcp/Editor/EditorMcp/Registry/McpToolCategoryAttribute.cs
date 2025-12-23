using System;

namespace DatumStudios.EditorMCP.Registry
{
    /// <summary>
    /// Optional attribute to mark a class as belonging to a tool category.
    /// Used for organization and discovery grouping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpToolCategoryAttribute : Attribute
    {
        /// <summary>
        /// Gets the category name (e.g., "go", "project", "audio.mixer").
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Initializes a new instance of the McpToolCategoryAttribute class.
        /// </summary>
        /// <param name="category">The category name (e.g., "go", "project", "audio.mixer").</param>
        public McpToolCategoryAttribute(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty.", nameof(category));
            }

            Category = category;
        }
    }
}

