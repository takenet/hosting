using System.Threading.Tasks;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a type that execute a work.
    /// The <see cref="IWorker"/> Execution property is hot, meaning that the value is available since the instantiation.
    /// </summary>
    public interface IWorker : IStoppable
    {
        /// <summary>
        /// Gets the Task that represents the execution of the work.
        /// </summary>
        Task Execution { get; }
    }
}