using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using DatumStudios.EditorMCP.Schemas;

namespace DatumStudios.EditorMCP.Transport
{
    /// <summary>
    /// Dispatches tool invocations to the Unity main thread. Ensures all Unity API calls
    /// execute on the main thread, even when requests arrive from background transport threads.
    /// Uses EditorApplication.delayCall for queued execution (no Update() loops).
    /// </summary>
    public class EditorMcpMainThreadDispatcher
    {
        private static EditorMcpMainThreadDispatcher _instance;
        private readonly Queue<PendingWorkItem> _workQueue = new Queue<PendingWorkItem>();
        private readonly object _queueLock = new object();
        private bool _isProcessing;
        private static int _mainThreadId;
        private static bool _initialized;

        /// <summary>
        /// Gets the singleton instance of the dispatcher.
        /// </summary>
        public static EditorMcpMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EditorMcpMainThreadDispatcher();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets whether the current thread is the Unity main thread.
        /// Only reliable after initialization. Use at entry points (Invoke), not inside delayCall callbacks.
        /// </summary>
        public static bool IsMainThread => _initialized && Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Static constructor to register domain reload handlers and initialize main thread ID.
        /// Called on every domain reload, so we must unsubscribe first to prevent duplicates.
        /// CRITICAL: This runs AFTER domain reload completes, so we can safely reset _instance here.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // CRITICAL: Unsubscribe first to prevent duplicate registrations across domain reloads
            // This is safe even if not previously subscribed (no-op)
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // CRITICAL: Clear any stale delayCall handlers from previous domain
            // We do this BEFORE resetting _instance, using the old instance reference
            // This ensures we unsubscribe the old handler before it becomes inaccessible
            var oldInstance = _instance;
            if (oldInstance != null)
            {
                try
                {
                    // Unsubscribe the old instance's handler
                    EditorApplication.delayCall -= oldInstance.ProcessNextWorkItem;
                }
                catch
                {
                    // Ignore - instance might be stale, which is fine
                }
                
                // Now safe to reset instance (domain reload is complete, no more handlers can execute)
                _instance = null;
            }
            
            // Capture main thread ID on initialization (always on main thread via InitializeOnLoadMethod)
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _initialized = true;

            // Register domain reload handlers
            // Use CompilationPipeline.compilationStarted (available in 2022.3+)
            // Note: AssemblyReloadEvents may not be available in all Unity versions, so we use CompilationPipeline
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            
            // Additional safety: Also handle play mode state changes (domain reload can happen on play mode entry)
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Initializes a new instance of the EditorMcpMainThreadDispatcher class.
        /// </summary>
        private EditorMcpMainThreadDispatcher()
        {
            // Instance initialization - main thread ID already captured in static Initialize()
        }

        /// <summary>
        /// Clears the work queue when compilation starts (domain reload imminent).
        /// </summary>
        private static void OnCompilationStarted(object obj)
        {
            ClearWorkQueue();
        }

        /// <summary>
        /// Additional safety: Clear queue when entering play mode (domain reload can occur).
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ClearWorkQueue();
            }
        }

        /// <summary>
        /// Clears the work queue and resets processing state.
        /// CRITICAL: Also unsubscribes from delayCall to prevent stale handlers from executing during/after domain reload.
        /// NOTE: We do NOT set _instance = null here because delayCall handlers might still reference it.
        /// Instead, we let Initialize() handle instance cleanup and reset.
        /// </summary>
        private static void ClearWorkQueue()
        {
            // Capture instance reference to avoid race conditions
            var instance = _instance;
            if (instance != null)
            {
                lock (instance._queueLock)
                {
                    // CRITICAL: Unsubscribe from delayCall to prevent stale handlers during domain reload
                    // delayCall handlers persist across domain reloads, so we must explicitly remove them
                    try
                    {
                        EditorApplication.delayCall -= instance.ProcessNextWorkItem;
                    }
                    catch
                    {
                        // Ignore - instance might be stale, which is fine (we'll clean up in Initialize)
                    }
                    
                    // Clear all pending work items and reset state
                    while (instance._workQueue.Count > 0)
                    {
                        var item = instance._workQueue.Dequeue();
                        item.CompletionEvent?.Set(); // Signal completion to prevent waiting threads from hanging
                    }
                    instance._isProcessing = false;
                }
                
                // DO NOT set _instance = null here!
                // delayCall handlers from previous domain might still reference the old instance.
                // Setting it to null here could cause NullReferenceException if those handlers execute.
                // Instead, let Initialize() handle instance reset after domain reload completes.
            }
        }

        /// <summary>
        /// Invokes work on the Unity main thread and waits for completion (with timeout).
        /// This method can be called from any thread. The work function will execute on the main thread.
        /// 
        /// Main-thread check: Only performed at entry point (here), not inside ProcessNextWorkItem.
        /// This prevents infinite loops during domain reload while maintaining safety.
        /// </summary>
        /// <param name="work">The work function to execute on the main thread.</param>
        /// <param name="timeout">Maximum time to wait for completion. Default: 30 seconds.</param>
        /// <returns>The tool invocation response, or a timeout error response if the timeout is exceeded.</returns>
        public ToolInvokeResponse Invoke(Func<ToolInvokeResponse> work, TimeSpan? timeout = null)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);

            // Main-thread check at entry point
            bool callerIsMainThread = IsMainThread;

            // If caller is already on main thread, execute immediately for synchronous behavior
            // This allows tests and synchronous callers to work without blocking
            if (callerIsMainThread)
            {
                try
                {
                    return work();
                }
                catch (Exception ex)
                {
                    return CreateErrorResponse($"Tool execution failed: {ex.Message}", ex);
                }
            }

            // Otherwise, enqueue work for main thread execution
            var workItem = new PendingWorkItem
            {
                Work = work,
                CompletionEvent = new ManualResetEventSlim(false),
                Result = null,
                Exception = null,
                EnqueuedAt = Stopwatch.GetTimestamp()
            };

            lock (_queueLock)
            {
                _workQueue.Enqueue(workItem);
                
                // Schedule processing if not already scheduled
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    EditorApplication.delayCall += ProcessNextWorkItem;
                }
            }

            // Wait for completion (with timeout)
            bool completed = workItem.CompletionEvent.Wait(timeoutValue);

            if (!completed)
            {
                // Timeout - remove from queue if still pending
                lock (_queueLock)
                {
                    // Try to remove if still in queue (best effort)
                    var items = new List<PendingWorkItem>(_workQueue);
                    items.Remove(workItem);
                    _workQueue.Clear();
                    foreach (var item in items)
                    {
                        _workQueue.Enqueue(item);
                    }
                }

                return CreateTimeoutResponse(timeoutValue);
            }

            // Check for exception
            if (workItem.Exception != null)
            {
                return CreateErrorResponse($"Tool execution failed: {workItem.Exception.Message}", workItem.Exception);
            }

            // Return result
            return workItem.Result ?? CreateErrorResponse("Tool execution returned null result", null);
        }

        /// <summary>
        /// Processes the next work item from the queue. Called via EditorApplication.delayCall.
        /// 
        /// CRITICAL: EditorApplication.delayCall is ALWAYS invoked on the main thread per Unity docs.
        /// We trust delayCall and do NOT check thread ID here to avoid infinite loops during domain reload.
        /// Main-thread checks are only performed at entry points (Invoke method), not in queue consumers.
        /// 
        /// CRITICAL: Check if this instance is still the current instance to prevent stale handlers from executing.
        /// </summary>
        private void ProcessNextWorkItem()
        {
            // CRITICAL: Check if this instance is still the current instance
            // If _instance was reset to null or changed during domain reload, this handler is stale
            // and should not execute (prevents infinite loops and NullReferenceExceptions)
            if (_instance != this)
            {
                // This handler is from a previous domain - ignore and don't re-queue
                return;
            }

            PendingWorkItem workItem = null;

            lock (_queueLock)
            {
                // Double-check instance is still valid (might have changed during lock acquisition)
                if (_instance != this)
                {
                    return;
                }

                if (_workQueue.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }

                workItem = _workQueue.Dequeue();
            }

            // Execute work on main thread (we trust delayCall is on main thread)
            var stopwatch = Stopwatch.StartNew();
            try
            {
                workItem.Result = workItem.Work();
                workItem.ElapsedMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                workItem.Exception = ex;
                workItem.ElapsedMs = stopwatch.ElapsedMilliseconds;
            }
            finally
            {
                // Signal completion
                workItem.CompletionEvent.Set();
            }

            // Schedule next item if queue is not empty
            lock (_queueLock)
            {
                if (_workQueue.Count > 0)
                {
                    EditorApplication.delayCall += ProcessNextWorkItem;
                }
                else
                {
                    _isProcessing = false;
                }
            }
        }

        /// <summary>
        /// Creates an error response for tool execution failures.
        /// </summary>
        private static ToolInvokeResponse CreateErrorResponse(string message, Exception exception)
        {
            return new ToolInvokeResponse
            {
                Tool = "internal.error",
                Output = new Dictionary<string, object>
                {
                    { "error", message },
                    { "exceptionType", exception?.GetType().Name ?? "Unknown" }
                },
                Diagnostics = exception != null ? new[] { exception.StackTrace } : null
            };
        }

        /// <summary>
        /// Creates a timeout error response.
        /// </summary>
        private static ToolInvokeResponse CreateTimeoutResponse(TimeSpan timeout)
        {
            return new ToolInvokeResponse
            {
                Tool = "internal.timeout",
                Output = new Dictionary<string, object>
                {
                    { "error", "Tool execution timed out" },
                    { "timeoutSeconds", timeout.TotalSeconds }
                },
                Diagnostics = new[] { $"Tool execution exceeded timeout of {timeout.TotalSeconds} seconds" }
            };
        }

        /// <summary>
        /// Represents a pending work item in the queue.
        /// </summary>
        private class PendingWorkItem
        {
            public Func<ToolInvokeResponse> Work { get; set; }
            public ManualResetEventSlim CompletionEvent { get; set; }
            public ToolInvokeResponse Result { get; set; }
            public Exception Exception { get; set; }
            public long EnqueuedAt { get; set; } // Stopwatch timestamp for batch safety
            public long ElapsedMs { get; set; } // Execution time in milliseconds
        }
    }
}

