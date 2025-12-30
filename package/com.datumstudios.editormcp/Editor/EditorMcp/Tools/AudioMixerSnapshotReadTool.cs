using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Tools
{
    /// <summary>
    /// Tool: audio.mixer.snapshot.read - Returns the parameter values for a specific AudioMixer snapshot.
    /// </summary>
    [McpToolCategory("audio")]
    public static class AudioMixerSnapshotReadTool
    {
        /// <summary>
        /// Returns the parameter values for a specific AudioMixer snapshot. Shows structured, numeric tooling without mutation.
        /// </summary>
        /// <param name="jsonParams">JSON parameters with "mixerPath" or "mixerGuid" and "snapshotName".</param>
        /// <returns>Dictionary with snapshot parameters.</returns>
        [McpTool("audio.mixer.snapshot.read", "Returns parameter values for a specific AudioMixer snapshot. Shows structured, numeric tooling without mutation.", Tier.Core)]
        public static Dictionary<string, object> Invoke(string jsonParams)
        {
            string mixerPath = null;
            string mixerGuid = null;
            string snapshotName = null;

            // Parse JSON parameters
            if (!string.IsNullOrEmpty(jsonParams) && jsonParams != "{}")
            {
                var paramsObj = UnityEngine.JsonUtility.FromJson<Dictionary<string, object>>(jsonParams);
                if (paramsObj != null)
                {
                    if (paramsObj.TryGetValue("mixerPath", out var mixerPathObj) && mixerPathObj is string)
                    {
                        mixerPath = (string)mixerPathObj;
                    }

                    if (paramsObj.TryGetValue("mixerGuid", out var mixerGuidObj) && mixerGuidObj is string)
                    {
                        mixerGuid = (string)mixerGuidObj;
                    }

                    if (paramsObj.TryGetValue("snapshotName", out var snapshotNameObj) && snapshotNameObj is string)
                    {
                        snapshotName = (string)snapshotNameObj;
                    }
                }
            }

            // Guard: Only process assets in Assets/ folder (never touch Packages/)
            if (string.IsNullOrEmpty(mixerPath) || !mixerPath.StartsWith("Assets/"))
            {
                var errorResult = new Dictionary<string, object>
                {
                    { "error", $"AudioMixer path must be in Assets/ folder. Package assets are not supported." }
                };
                return errorResult;
            }

            // Load mixer
            AudioMixer mixer = null;
            try
            {
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            }
            catch (System.Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to load AudioMixer: {ex.Message}" }
                };
            }

            if (mixer == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"AudioMixer at path '{mixerPath}' not found or is not an AudioMixer asset" }
                };
            }

            // Find snapshot
            AudioMixerSnapshot snapshot = null;
            try
            {
                // Try to find snapshot by name
                snapshot = FindSnapshotByName(mixer, snapshotName);
            }
            catch (System.Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to find snapshot: {ex.Message}" },
                    { "diagnostics", new string[] { "Unity AudioMixer API may have limitations accessing snapshots" } }
                };
            }

            if (snapshot == null)
            {
                var errorResult = new Dictionary<string, object>
                {
                    { "error", $"Snapshot '{snapshotName}' not found in mixer '{mixerPath}'" }
                };
                return errorResult;
            }

            // Read parameters (best-effort)
            var parameters = new List<Dictionary<string, object>>();
            var diagnostics = new List<string>();

            try
            {
                // Get exposed parameters from the mixer
                var exposedParams = GetExposedParameters(mixer);
                
                // Read snapshot parameter values using SerializedObject
                var snapshotSo = new SerializedObject(snapshot);
                var snapshotValuesProperty = snapshotSo.FindProperty("m_SnapshotValues");
                
                if (snapshotValuesProperty != null && snapshotValuesProperty.isArray)
                {
                    // Build a map of parameter GUID to value
                    var paramValueMap = new Dictionary<string, float>();
                    
                    for (int i = 0; i < snapshotValuesProperty.arraySize; i++)
                    {
                        var valueElement = snapshotValuesProperty.GetArrayElementAtIndex(i);
                        var guidProperty = valueElement.FindPropertyRelative("guid");
                        var valueProperty = valueElement.FindPropertyRelative("value");
                        
                        if (guidProperty != null && valueProperty != null)
                        {
                            paramValueMap[guidProperty.stringValue] = valueProperty.floatValue;
                        }
                    }
                    
                    // Match exposed parameters with their values
                    var exposedParamsWithGuids = GetExposedParametersWithGuids(mixer);
                    
                    foreach (var paramInfo in exposedParamsWithGuids)
                    {
                        var paramName = paramInfo.Key;
                        var paramGuid = paramInfo.Value;
                        
                        if (paramValueMap.TryGetValue(paramGuid, out float value))
                        {
                            // Try to determine which group this parameter belongs to
                            string groupName = GetParameterGroup(mixer, paramName);
                            
                            parameters.Add(new Dictionary<string, object>
                            {
                                { "name", paramName },
                                { "value", value },
                                { "group", groupName ?? "Unknown" }
                            });
                        }
                        else
                        {
                            diagnostics.Add($"Parameter '{paramName}' exists but value not found in snapshot");
                        }
                    }
                }
                else
                {
                    diagnostics.Add("Snapshot values property not found - Unity API limitation");
                }

                // Sort for deterministic ordering
                parameters = parameters.OrderBy(p => (string)p["group"]).ThenBy(p => (string)p["name"]).ToList();
            }
            catch (System.Exception ex)
            {
                diagnostics.Add($"Error reading parameters: {ex.Message}");
            }

            var output = new Dictionary<string, object>
            {
                { "mixerPath", mixerPath },
                { "snapshotName", snapshotName },
                { "parameters", parameters.ToArray() }
            };

            // Add note to diagnostics if there are any issues
            if (diagnostics.Count > 0)
            {
                diagnostics.Add("Some parameter values may not be available due to Unity API limitations. This is a best-effort read operation.");
                output["diagnostics"] = diagnostics.ToArray();
            }

            return output;
        }

        private static AudioMixerSnapshot FindSnapshotByName(AudioMixer mixer, string snapshotName)
        {
            try
            {
                // Use SerializedObject to access all snapshots
                var so = new SerializedObject(mixer);
                var snapshotsProperty = so.FindProperty("m_Snapshots");
                
                if (snapshotsProperty != null && snapshotsProperty.isArray)
                {
                    for (int i = 0; i < snapshotsProperty.arraySize; i++)
                    {
                        var snapshotElement = snapshotsProperty.GetArrayElementAtIndex(i);
                        var snapshot = snapshotElement.objectReferenceValue as AudioMixerSnapshot;
                        if (snapshot != null && snapshot.name == snapshotName)
                        {
                            return snapshot;
                        }
                    }
                }
            }
            catch
            {
                // Fallback: try FindSnapshot (may not work for all cases)
                var snapshot = mixer.FindSnapshot(snapshotName);
                if (snapshot != null && snapshot.name == snapshotName)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private static List<string> GetExposedParameters(AudioMixer mixer)
        {
            var parameters = new List<string>();

            try
            {
                // Use SerializedObject to get exposed parameters
                var so = new SerializedObject(mixer);
                var exposedParamsProperty = so.FindProperty("m_ExposedParameters");
                
                if (exposedParamsProperty != null && exposedParamsProperty.isArray)
                {
                    for (int i = 0; i < exposedParamsProperty.arraySize; i++)
                    {
                        var paramElement = exposedParamsProperty.GetArrayElementAtIndex(i);
                        var nameProperty = paramElement.FindPropertyRelative("name");
                        if (nameProperty != null)
                        {
                            parameters.Add(nameProperty.stringValue);
                        }
                    }
                }
            }
            catch
            {
                // Best-effort: if we can't get exposed parameters, return empty list
            }

            return parameters;
        }

        private static Dictionary<string, string> GetExposedParametersWithGuids(AudioMixer mixer)
        {
            var parameters = new Dictionary<string, string>();

            try
            {
                // Use SerializedObject to get exposed parameters with their GUIDs
                var so = new SerializedObject(mixer);
                var exposedParamsProperty = so.FindProperty("m_ExposedParameters");
                
                if (exposedParamsProperty != null && exposedParamsProperty.isArray)
                {
                    for (int i = 0; i < exposedParamsProperty.arraySize; i++)
                    {
                        var paramElement = exposedParamsProperty.GetArrayElementAtIndex(i);
                        var nameProperty = paramElement.FindPropertyRelative("name");
                        var guidProperty = paramElement.FindPropertyRelative("guid");
                        
                        if (nameProperty != null && guidProperty != null)
                        {
                            parameters[nameProperty.stringValue] = guidProperty.stringValue;
                        }
                    }
                }
            }
            catch
            {
                // Best-effort: if we can't get exposed parameters, return empty dictionary
            }

            return parameters;
        }

        private static string GetParameterGroup(AudioMixer mixer, string parameterName)
        {
            try
            {
                // Try to find which group this parameter belongs to
                // This is best-effort as Unity API doesn't directly expose this
                var groups = mixer.FindMatchingGroups("");
                if (groups != null)
                {
                    foreach (var group in groups)
                    {
                        // Check if parameter name contains group name (heuristic)
                        if (parameterName.Contains(group.name))
                        {
                            return group.name;
                        }
                    }
                }
            }
            catch
            {
                // Best-effort: return null if we can't determine
            }

            return null;
        }
    }
}

