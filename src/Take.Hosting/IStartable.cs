using System.Threading;
using System.Threading.Tasks;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a service that can be started.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Starts the execution of the service.
        /// </summary>
        /// <param name="cancellationToken">Token to allow the abortion of the service start</param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}