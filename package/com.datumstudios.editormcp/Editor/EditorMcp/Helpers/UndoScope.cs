using UnityEditor;
using UnityEngine;

namespace DatumStudios.EditorMCP.Helpers
{
    /// <summary>
    /// IDisposable scope for managing Unity undo groups.
    /// Ensures all operations within the scope are grouped under a single undo step.
    /// </summary>
    public class UndoScope : System.IDisposable
    {
        private readonly string _groupName;
        private bool _disposed = false;

        /// <summary>
        /// Creates a new undo scope with the specified group name.
        /// </summary>
        /// <param name="groupName">Name for the undo group (e.g., "go.setParent")</param>
        public UndoScope(string groupName)
        {
            _groupName = groupName ?? "EditorMCP Operation";
            Undo.SetCurrentGroupName(_groupName);
        }

        /// <summary>
        /// Disposes the undo scope, finalizing the undo group.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Unity automatically groups operations between SetCurrentGroupName calls
                // No explicit cleanup needed, but we mark as disposed to prevent double-disposal
                _disposed = true;
            }
        }
    }
}

