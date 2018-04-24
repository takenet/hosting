namespace Take.Hosting
{
    /// <summary>
    /// Defines a uniform service hosting infrastructure to control the lifecycle of multiple <see cref="IService"/> instances.
    /// </summary>
    public interface IServiceContainer : IService
    {
        /// <summary>
        /// Adds a service to the container.
        /// </summary>
        /// <param name="service">The service instance.</param>
        /// <param name="tier">The service tier, that can be used to create dependencies between services. Lower tier services are started first and stopped last. Services in the same tier can be started at the same time.</param>
        IServiceContainer Add(IService service, int tier = 0);

        /// <summary>
        /// Gets the total of registered services.
        /// </summary>
        int Count { get; }
    }
}