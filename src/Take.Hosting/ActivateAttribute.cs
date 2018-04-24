using System;

namespace Take.Hosting
{
    /// <summary>
    /// Indicates that the <see cref="IService"/> class should be activated on the server initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ActivateAttribute : Attribute, IActivatable
    {
        public const string DEFAULT_GROUP_NAME = "Default";

        public ActivateAttribute()
        {
            Group = DEFAULT_GROUP_NAME;
            Tier = ServiceActivator.DEFAULT_TIER;
        }

        /// <summary>
        /// Gets or sets the type of the registrable that allows the service register its own dependencies in an isolate container.
        /// The type must implement <see cref="IRegistrable"/>.
        /// </summary>
        public Type RegistrableType { get; set; }

        /// <summary>
        /// Indicates if the diagnostics types (ILogger, IExceptionHandler and ITracer) registrations made by the service should be keep and not overridden by the activator.
        /// </summary>
        public bool PreserveDiagnostics { get; set; }

        /// <summary>
        /// Determine the prefix for the configuration value provider keys.
        /// </summary>
        public string ConfigurationValuePrefix { get; set; }

        /// <summary>
        /// Gets or sets the service tier, which determines the activation order of the service.
        /// </summary>
        public int Tier { get; set; }

        /// <summary>
        /// Gets or sets the service activation group name.
        /// </summary>
        public string Group { get; set; }
    }
}