using UnityEngine;
using UnityEditor;

namespace DatumStudios.EditorMCP.Diagnostics
{
    /// <summary>
    /// Validates Unity version compatibility for EditorMCP.
    /// Ensures Unity 2022.3 LTS minimum requirement is met.
    /// </summary>
    public static class VersionValidator
    {
        private const string MIN_VERSION = "2022.3.0f1";
        private const string PACKAGE_NAME = "EditorMCP";
        
        /// <summary>
        /// Validates Unity version on package load.
        /// Logs error if version is below minimum requirement.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void ValidateOnLoad()
        {
            if (!IsCompatible())
            {
                Debug.LogError(
                    $"[{PACKAGE_NAME}] Requires Unity {MIN_VERSION}+. " +
                    $"Current version: {Application.unityVersion}. " +
                    $"Please upgrade to Unity 2022.3 LTS or later."
                );
            }
        }
        
        /// <summary>
        /// Checks if current Unity version is compatible with EditorMCP.
        /// </summary>
        /// <returns>True if Unity version is 2022.3 or later, false otherwise.</returns>
        public static bool IsCompatible()
        {
            return VersionCompare(Application.unityVersion, MIN_VERSION) >= 0;
        }
        
        /// <summary>
        /// Gets the minimum required Unity version.
        /// </summary>
        /// <returns>Minimum Unity version string (e.g., "2022.3.0f1").</returns>
        public static string GetMinimumVersion()
        {
            return MIN_VERSION;
        }
        
        /// <summary>
        /// Compares two Unity version strings.
        /// </summary>
        /// <param name="versionA">First version string (e.g., "2022.3.20f1").</param>
        /// <param name="versionB">Second version string (e.g., "2022.3.0f1").</param>
        /// <returns>Negative if A < B, zero if A == B, positive if A > B.</returns>
        private static int VersionCompare(string versionA, string versionB)
        {
            var partsA = ParseVersion(versionA);
            var partsB = ParseVersion(versionB);
            
            // Compare major, minor, patch, build
            for (int i = 0; i < Mathf.Max(partsA.Length, partsB.Length); i++)
            {
                int partA = i < partsA.Length ? partsA[i] : 0;
                int partB = i < partsB.Length ? partsB[i] : 0;
                
                if (partA != partB)
                {
                    return partA.CompareTo(partB);
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// Parses Unity version string into integer array.
        /// Handles formats like "2022.3.20f1" or "6000.0.0f1".
        /// </summary>
        /// <param name="version">Unity version string.</param>
        /// <returns>Array of version parts [major, minor, patch, build].</returns>
        private static int[] ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new int[] { 0, 0, 0, 0 };
            }
            
            // Remove 'f' suffix (e.g., "20f1" -> "201")
            string cleaned = version.Replace("f", "");
            
            // Split by '.' and parse each part
            string[] parts = cleaned.Split('.');
            int[] result = new int[4]; // [major, minor, patch, build]
            
            for (int i = 0; i < Mathf.Min(parts.Length, result.Length); i++)
            {
                // Extract numeric part (e.g., "20f1" -> "201" -> 201)
                string numericPart = System.Text.RegularExpressions.Regex.Replace(parts[i], @"[^0-9]", "");
                if (int.TryParse(numericPart, out int value))
                {
                    result[i] = value;
                }
            }
            
            return result;
        }
    }
}

