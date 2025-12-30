using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DatumStudios.EditorMCP.Transport
{
    /// <summary>
    /// Writes line-delimited JSON to a stream. Writes exactly one JSON object per line
    /// and flushes immediately for real-time communication.
    /// </summary>
    public class LineJsonWriter : IDisposable
    {
        private readonly StreamWriter _writer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the LineJsonWriter class.
        /// </summary>
        /// <param name="stream">The output stream to write to.</param>
        public LineJsonWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Create StreamWriter without BOM (UTF8NoBOM) to avoid BOM issues in loopback tests
            // Use UTF8 encoding without BOM for cleaner output
            var utf8NoBom = new System.Text.UTF8Encoding(false); // false = no BOM
            _writer = new StreamWriter(stream, utf8NoBom, 4096, true)
            {
                AutoFlush = true // Flush immediately for real-time communication
            };
        }

        /// <summary>
        /// Writes a JSON object as a single line, followed by a newline.
        /// </summary>
        /// <param name="obj">The object to serialize and write.</param>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentException">Thrown when serialization fails.</exception>
        public void WriteLine(object obj)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LineJsonWriter));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // Dictionary<string, object> needs manual serialization (JsonUtility returns empty {})
            if (obj is Dictionary<string, object> dict)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"\"{kvp.Key}\":");
                    var value = kvp.Value;
                    if (value is string strValue)
                        sb.Append($"\"{strValue}\"");
                    else if (value is int || value is long || value is float || value is double || value is bool)
                        sb.Append(value);
                    else
                        sb.Append($"\"{value}\"");
                }
                sb.Append("}");
                var manualJson = sb.ToString();
                _writer.Write(manualJson);
                _writer.Write("\n");
                _writer.Flush();
            }
            // Handle string directly
            else if (obj is string jsonString)
            {
                _writer.Write(jsonString);
                _writer.Write("\n");
                _writer.Flush();
            }
            // Try JsonUtility for other objects (works for simple types)
            else
            {
                string json = JsonUtility.ToJson(obj, prettyPrint: false);
                _writer.Write(json);
                _writer.Write("\n");
                _writer.Flush();
            }
        }

        /// <summary>
        /// Writes a JSON string directly as a line.
        /// </summary>
        /// <param name="json">The JSON string to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
        public void WriteLine(string json)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LineJsonWriter));

            if (json == null)
                throw new ArgumentNullException(nameof(json));

            _writer.Write(json);
            _writer.Write("\n");
            _writer.Flush(); // Ensure immediate write
        }

        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
        public void Flush()
        {
            if (!_disposed)
            {
                _writer?.Flush();
            }
        }

        /// <summary>
        /// Disposes the writer and underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _disposed = true;
            }
        }
    }
}

