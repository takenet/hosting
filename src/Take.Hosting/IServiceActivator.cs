using System;
using System.Collections.Generic;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a service for activating other services. 
    /// </summary>
    public interface IServiceActivator : IService
    {
        /// <summary>
        /// Gets the associated IServiceProvider for each registered service.
        /// </summary>
        IDictionary<Type, IServiceProvider> ServiceProviders { get; }

        /// <summary>
        /// Gets the group names for activation.
        /// </summary>
        ICollection<string> ActivationGroups { get; }

        /// <summary>
        /// Gets the types which should be overwritten in the activated service containers by parent container instances.
        /// </summary>        
        ICollection<Type> OverrideTypes { get; }
    }
}