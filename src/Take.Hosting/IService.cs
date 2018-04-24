namespace Take.Hosting
{
    /// <summary>
    /// Defines a worker that can be started.
    /// The <see cref="IService"/> Execution property is cold, meaning that the value is available only after the StartAsync method is called.
    /// </summary>
    public interface IService : IWorker, IStartable
    {

    }
}