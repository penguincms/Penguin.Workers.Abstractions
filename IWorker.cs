using System;

namespace Penguin.Workers.Abstractions
{
    /// <summary>
    /// An interface required to hook a class into the Worker system.
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Denoted whether or not the worker is currently running
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// The last time the worker was run
        /// </summary>
        DateTime LastRun { get; }

        /// <summary>
        /// The root path for the application launching the worker
        /// </summary>
        string WorkerRoot { get; set; }

        /// <summary>
        /// The code to execute when the worker is run
        /// </summary>
        void Run();

        /// <summary>
        /// Code to launch the worker Async
        /// </summary>
        /// <param name="force">Optional parameter to force the worker to run</param>
        void UpdateAsync(bool force = false);

        /// <summary>
        /// Call this after the worker has run and make it update the last run time wherever that is stored
        /// </summary>
        void UpdateLastRun();

        /// <summary>
        /// Code to launch the worker synchronously
        /// </summary>
        /// <param name="force">Optional parameter to force the worker to run</param>
        void UpdateSync(bool force = false);
    }
}