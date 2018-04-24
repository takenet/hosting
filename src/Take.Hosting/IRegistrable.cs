using SimpleInjector;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a registration service.
    /// </summary>
    public interface IRegistrable
    {
        /// <summary>
        /// Registers to the specified container.
        /// The container is exclusive for every IService instance, but is linked to a parent container for resolving not found types.
        /// </summary>
        /// <param name="container">The container.</param>
        void RegisterTo(Container container);
    }
}