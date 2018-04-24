using System;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a monitor for services execution tasks which restarts it when they became faulted.
    /// </summary>
    public interface IServiceMonitor
    {
        /// <summary>
        /// Attaches a monitoring task to the specified service.
        /// </summary>
        /// <param name="service">The service to monitor.</param>
        /// <param name="delay">The delay to restart the service when it became faulted</param>
        /// <returns></returns>
        IDisposable Monitor(IService service, TimeSpan delay);
    }
}