using System;
using System.Collections.Generic;
using System.Text;

namespace Take.Hosting
{
    /// <summary>
    /// Provides information about an activatable service.
    /// </summary>
    public interface IActivatable
    {
        /// <summary>
        /// Gets or sets the type of the registrable that allows the service register its own dependencies in an isolate container.
        /// </summary>
        Type RegistrableType { get; }

        /// <summary>
        /// Indicates if the diagnostics types (ILogger, IExceptionHandler and ITracer) registrations made by the service should be keep and not overridden by the activator.
        /// </summary>
        bool PreserveDiagnostics { get; }

        /// <summary>
        /// Determine the prefix for the configuration value provider keys.
        /// </summary>
        string ConfigurationValuePrefix { get; }

        /// <summary>
        /// Gets or sets the service tier, which determines the activation order of the service.
        /// </summary>
        int Tier { get; }

        /// <summary>
        /// Gets or sets the service activation group name.
        /// </summary>
        string Group { get; }
    }
}
