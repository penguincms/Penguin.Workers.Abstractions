using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;

namespace Penguin.Workers.Abstractions
{
    /// <summary>
    /// The lowest implementation of a Worker object designed to create minumum functionality expected
    /// </summary>
    public abstract class Worker : IWorker
    {
        /// <summary>
        /// Directory where the configuration files for the worker should be stored
        /// </summary>
        public string ConfigDirectory => Path.Combine(WorkerRoot, "Configs");

        /// <summary>
        /// Represents the time between worker runs
        /// </summary>
        public abstract TimeSpan Delay { get; }

        /// <summary>
        /// Is the worker currently running
        /// </summary>
        public bool IsBusy { get; set; }

        /// <summary>
        /// The last run time if the worker has been kept in memory
        /// </summary>
        public DateTime LastRun { get; protected set; }

        /// <summary>
        /// The root path for the application currently that has this DLL loaded, and the base directory for all workers
        /// </summary>
        public virtual string WorkerRoot
        {
            get
            {
                _workerRoot ??= Directory.GetCurrentDirectory();

                return _workerRoot;
            }
            set => _workerRoot = value;
        }

        internal bool AllowAsync { get; set; } = true;

        private string _workerRoot { get; set; }

        private BackgroundWorker BackgroundWorker { get; set; }

        /// <summary>
        /// Creates a new instance of the worker class
        /// </summary>
        protected Worker()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                _ = Directory.CreateDirectory(ConfigDirectory);
            }

            BackgroundWorker = new BackgroundWorker();
            BackgroundWorker.DoWork += Worker_DoWork;
        }

        /// <summary>
        /// Retrieves a class instance of the worker configuration from the serialized file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfiguration<T>() where T : WorkerConfiguration
        {
            string ConfigName = Path.Combine(ConfigDirectory, $"{GetType().Name}.localConfig");

            if (!File.Exists(ConfigName))
            {
                File.AppendAllText(ConfigName, JsonConvert.SerializeObject(Activator.CreateInstance<T>(), Formatting.Indented));
            }

            T Configuration = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigName));

            return !Configuration.Configured ? throw new Exception("Configuration file not populated: " + ConfigName) : Configuration;
        }

        /// <summary>
        /// Attempts to run the worker, if its not currently running
        /// </summary>
        public void Run(params string[] args)
        {
            if (!BackgroundWorker.IsBusy)
            {
                LastRun = DateTime.Now;
                BackgroundWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Method to contain the logic that the worker executes
        /// </summary>
        public abstract void RunWorker(params string[] args);

        /// <summary>
        /// Converts a class representation of the worker configuration into a string and saves it to a file
        /// </summary>
        /// <typeparam name="T">Any WorkerConfiguration type</typeparam>
        /// <param name="configuration">The configuration to save</param>
        public void SaveConfiguration<T>(T configuration) where T : WorkerConfiguration
        {
            string ConfigName = Path.Combine(ConfigDirectory, $"{GetType().Name}.localConfig");

            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }

        /// <summary>
        /// Run the worker Async
        /// </summary>
        /// <param name="force">Force the worker to run if ahead of schedule</param>
        /// <param name="args"></param>
        public void UpdateAsync(bool force = false, params string[] args)
        {
            if (!BackgroundWorker.IsBusy && (LastRun + Delay < DateTime.Now || LastRun == DateTime.MinValue || force))
            {
                LastRun = DateTime.Now;
                BackgroundWorker.RunWorkerAsync(args);
            }
        }

        /// <summary>
        /// Placeholder for method used to update the last run time. Storage location depends on implementation
        /// </summary>
        public abstract void UpdateLastRun();

        /// <summary>
        /// Runs the worker synchronously
        /// </summary>
        /// <param name="force">Force the worker to run, if before the next scheduled time</param>
        /// <param name="args"></param>
        public void UpdateSync(bool force = false, params string[] args)
        {
            if (LastRun + Delay < DateTime.Now || LastRun == DateTime.MinValue || force)
            {
                LastRun = DateTime.Now;
                Worker_DoWork(null, new DoWorkEventArgs(args));
            }
        }

        /// <summary>
        /// Event handler to set the worker to busy and run. Use Update or UpdateSync
        /// </summary>
        /// <param name="sender">Not Used</param>
        /// <param name="e">Not Used</param>
        public virtual void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            try
            {
                IsBusy = true;
                RunWorker(e.Argument as string[]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}