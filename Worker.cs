using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;

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
        public string ConfigDirectory => Path.Combine(this.WorkerRoot, "Configs");

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
                if (_workerRoot is null)
                {
                    _workerRoot = Directory.GetCurrentDirectory();
                }

                return _workerRoot;
            }
            set
            {
                _workerRoot = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the worker class
        /// </summary>
        public Worker()
        {
            if (!Directory.Exists(this.ConfigDirectory))
            {
                Directory.CreateDirectory(this.ConfigDirectory);
            }

            this.BackgroundWorker = new BackgroundWorker();
            this.BackgroundWorker.DoWork += this.Worker_DoWork;
        }

        /// <summary>
        /// Retrieves a class instance of the worker configuration from the serialized file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfiguration<T>() where T : WorkerConfiguration
        {
            string ConfigName = Path.Combine(this.ConfigDirectory, $"{this.GetType().Name}.localConfig");

            if (!File.Exists(ConfigName))
            {
                File.AppendAllText(ConfigName, JsonConvert.SerializeObject(Activator.CreateInstance<T>(), Formatting.Indented));
            }

            T Configuration = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigName));

            if (!Configuration.Configured)
            {
                throw new Exception("Configuration file not populated: " + ConfigName);
            }

            return Configuration;
        }

        /// <summary>
        /// Attempts to run the worker, if its not currently running
        /// </summary>
        public void Run()
        {
            if (!this.BackgroundWorker.IsBusy)
            {
                this.LastRun = DateTime.Now;
                this.BackgroundWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Method to contain the logic that the worker executes
        /// </summary>
        public abstract void RunWorker();

        /// <summary>
        /// Converts a class representation of the worker configuration into a string and saves it to a file
        /// </summary>
        /// <typeparam name="T">Any WorkerConfiguration type</typeparam>
        /// <param name="configuration">The configuration to save</param>
        public void SaveConfiguration<T>(T configuration) where T : WorkerConfiguration
        {
            string ConfigName = Path.Combine(this.ConfigDirectory, $"{this.GetType().Name}.localConfig");

            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }

        /// <summary>
        /// Run the worker Async
        /// </summary>
        /// <param name="force">Force the worker to run if ahead of schedule</param>
        public void UpdateAsync(bool force = false)
        {
            if (!this.BackgroundWorker.IsBusy && (this.LastRun + this.Delay < DateTime.Now || this.LastRun == DateTime.MinValue || force))
            {
                this.LastRun = DateTime.Now;
                this.BackgroundWorker.RunWorkerAsync();
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
        public void UpdateSync(bool force = false)
        {
            if (this.LastRun + this.Delay < DateTime.Now || this.LastRun == DateTime.MinValue || force)
            {
                this.LastRun = DateTime.Now;
                this.Worker_DoWork(null, null);
            }
        }

        /// <summary>
        /// Event handler to set the worker to busy and run. Use Update or UpdateSync
        /// </summary>
        /// <param name="sender">Not Used</param>
        /// <param name="e">Not Used</param>
        public virtual void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.IsBusy = true;
                this.RunWorker();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        internal bool AllowAsync { get; set; } = true;
        private string _workerRoot { get; set; }
        private BackgroundWorker BackgroundWorker { get; set; }
    }
}