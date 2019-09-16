namespace Penguin.Workers.Abstractions
{
    /// <summary>
    /// The base class for a worker configuration, used to enforce settings before running
    /// </summary>
    public class WorkerConfiguration
    {
        /// <summary>
        /// A bool representing whether or not this worker configuration has been configured. Used to force the user to open and alter the configuration to prevent
        /// default values from being overlooked. Will throw an error if not true
        /// </summary>
        public bool Configured { get; set; }
    }
}