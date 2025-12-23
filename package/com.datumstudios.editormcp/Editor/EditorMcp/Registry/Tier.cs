namespace DatumStudios.EditorMCP.Registry
{
    /// <summary>
    /// Defines the license tier levels for EditorMCP tools.
    /// Tools are gated based on the user's license tier.
    /// </summary>
    public enum Tier
    {
        /// <summary>
        /// Core tier - Free, available to all users (GitHub).
        /// </summary>
        Core = 0,

        /// <summary>
        /// Pro tier - Asset Store license ($39).
        /// </summary>
        Pro = 1,

        /// <summary>
        /// Studio tier - Team workflows ($99 upgrade).
        /// </summary>
        Studio = 2,

        /// <summary>
        /// Enterprise tier - CI/CD and governance ($199 upgrade).
        /// </summary>
        Enterprise = 3
    }
}

