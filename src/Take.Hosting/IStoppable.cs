namespace Take.Hosting
{
    /// <summary>
    /// Defines a service that can be stopped.
    /// </summary>
    public interface IStoppable
    {
        /// <summary>
        /// Signals to stop the execution of the work. 
        /// The loop is stopped when the Execution task is completed.
        /// </summary>
        void Stop();
    }
}