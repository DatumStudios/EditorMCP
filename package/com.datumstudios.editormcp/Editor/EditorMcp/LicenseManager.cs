using UnityEditor;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP
{
    /// <summary>
    /// Manages license tier validation for EditorMCP tools.
    /// Uses Unity Asset Store utilities to check for package license.
    /// </summary>
    public static class LicenseManager
    {
        private const string PACKAGE_ID = "com.datumstudios.editormcp";
        private static bool? _isLicensed;

        /// <summary>
        /// Gets whether the user has a valid Asset Store license for EditorMCP.
        /// </summary>
        public static bool IsLicensed
        {
            get
            {
                if (_isLicensed == null)
                {
                    _isLicensed = AssetStoreUtils.HasLicenseForPackage(PACKAGE_ID);
                }
                return _isLicensed.Value;
            }
        }

        /// <summary>
        /// Checks if the user has access to the specified tier.
        /// </summary>
        /// <param name="minTier">The minimum tier required.</param>
        /// <returns>True if the user has access to the tier, false otherwise.</returns>
        public static bool HasTier(Tier minTier)
        {
            // Core tier is always available (free)
            if (minTier == Tier.Core)
            {
                return true;
            }

            // If not licensed, only Core tier is available
            if (!IsLicensed)
            {
                return false;
            }

            // v1.0: Licensed = Pro tier access
            // Future: Studio/Enterprise tiers will require additional license checks
            return minTier <= Tier.Pro;
        }

        /// <summary>
        /// Gets the current tier available to the user.
        /// </summary>
        /// <returns>The highest tier the user has access to.</returns>
        public static Tier CurrentTier
        {
            get
            {
                if (!IsLicensed)
                {
                    return Tier.Core;
                }

                // v1.0: Licensed = Pro tier
                // Future: Check for Studio/Enterprise upgrades
                return Tier.Pro;
            }
        }
    }
}

