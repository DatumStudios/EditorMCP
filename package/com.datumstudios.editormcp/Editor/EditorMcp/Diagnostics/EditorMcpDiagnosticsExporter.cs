using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using DatumStudios.EditorMCP.Registry;

namespace DatumStudios.EditorMCP.Diagnostics
{
    public static class EditorMcpDiagnosticsExporter
    {
        private const int MaxConsoleEntries = 200;
        private static List<ConsoleEntry> _consoleEntries;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _consoleEntries = new List<ConsoleEntry>();
            Application.logMessageReceived += OnLogMessage;
        }

        private static void OnLogMessage(string logString, string stackTrace, LogType type)
        {
            if (_consoleEntries == null)
            {
                return;
            }

            var entry = new ConsoleEntry
            {
                type = type.ToString(),
                message = logString,
                stackTrace = stackTrace
            };

            lock (_consoleEntries)
            {
                _consoleEntries.Add(entry);
                if (_consoleEntries.Count > MaxConsoleEntries)
                {
                    _consoleEntries.RemoveAt(0);
                }
            }
        }

        [MenuItem("Tools/EditorMCP/Export Diagnostics")]
        private static void ExportDiagnostics()
        {
            var diagnostics = new DiagnosticsData
            {
                unityVersion = Application.unityVersion,
                editorApplicationVersion = Application.unityVersion,
                timeUtc = DateTime.UtcNow.ToString("o"),
                projectPath = Application.dataPath,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
                defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                asmdefs = GetAssemblyDefinitions(),
                consoleEntries = GetConsoleEntries(),
                toolRegistry = GetToolRegistryInfo()
            };

            string json = EditorMcpJson.Serialize(diagnostics);
            string outputPath = Path.Combine(Application.temporaryCachePath, "EditorMCP");
            string filePath = Path.Combine(outputPath, "diagnostics.json");

            Directory.CreateDirectory(outputPath);
            WriteFileAtomically(filePath, json);

            Debug.Log($"[EditorMCP] Diagnostics exported to: {filePath}");
            EditorUtility.RevealInFinder(filePath);
        }

        private static List<AsmdefInfo> GetAssemblyDefinitions()
        {
            var asmdefs = new List<AsmdefInfo>();

            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.StartsWith("Packages/"))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                {
                    var content = File.ReadAllText(path);
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                    var name = nameMatch.Success ? nameMatch.Groups[1].Value : Path.GetFileNameWithoutExtension(path);

                    asmdefs.Add(new AsmdefInfo
                    {
                        name = name,
                        guid = guid,
                        path = path
                    });
                }
            }

            return asmdefs.OrderBy(a => a.name).ToList();
        }

        private static List<ConsoleEntry> GetConsoleEntries()
        {
            if (_consoleEntries == null)
            {
                return new List<ConsoleEntry>();
            }

            lock (_consoleEntries)
            {
                return new List<ConsoleEntry>(_consoleEntries);
            }
        }

        private static ToolRegistryInfo GetToolRegistryInfo()
        {
            var registry = ToolRegistry.Current;
            var info = new ToolRegistryInfo
            {
                discoveredToolCount = registry?.Count ?? 0,
                toolIds = new List<string>(),
                duplicates = new List<DuplicateToolInfo>()
            };

            if (registry != null)
            {
                var toolList = registry.List();
                info.toolIds = toolList.Select(t => t.Id).OrderBy(id => id).ToList();

                var idGroups = toolList.GroupBy(t => t.Id).Where(g => g.Count() > 1);
                foreach (var group in idGroups)
                {
                    var duplicateInfo = new DuplicateToolInfo
                    {
                        toolId = group.Key,
                        methods = new List<string>()
                    };

                    foreach (var tool in group)
                    {
                        duplicateInfo.methods.Add(tool.Name);
                    }

                    info.duplicates.Add(duplicateInfo);
                }
            }

            return info;
        }

        private static void WriteFileAtomically(string filePath, string content)
        {
            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, content, System.Text.Encoding.UTF8);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
    }

    [Serializable]
    public class DiagnosticsData
    {
        public string unityVersion;
        public string editorApplicationVersion;
        public string timeUtc;
        public string projectPath;
        public string buildTarget;
        public string scriptingBackend;
        public string defineSymbols;
        public List<AsmdefInfo> asmdefs;
        public List<ConsoleEntry> consoleEntries;
        public ToolRegistryInfo toolRegistry;
    }

    [Serializable]
    public class AsmdefInfo
    {
        public string name;
        public string guid;
        public string path;
    }

    [Serializable]
    public class ConsoleEntry
    {
        public string type;
        public string message;
        public string stackTrace;
    }

    [Serializable]
    public class ToolRegistryInfo
    {
        public int discoveredToolCount;
        public List<string> toolIds;
        public List<DuplicateToolInfo> duplicates;
    }

    [Serializable]
    public class DuplicateToolInfo
    {
        public string toolId;
        public List<string> methods;
    }
}
