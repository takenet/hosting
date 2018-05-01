using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using SimpleInjector;

namespace Take.Hosting
{
    /// <summary>
    /// Implements a <see cref="IService"/> activator infrastructure.
    /// </summary>
    /// <seealso cref="ServiceBase" />
    /// <seealso cref="IServiceActivator" />
    public class ServiceActivator : ServiceBase, IServiceActivator, IDisposable
    {
        public const int DEFAULT_TIER = 100;

        private readonly Container _parentContainer;
        private readonly ILogger _logger;
        private readonly IServiceContainer _serviceContainer;
        private readonly IConfigurationValueProvider _configurationValueProvider;

        public ServiceActivator(
            Container parentContainer,
            ILogger logger,
            IServiceContainer serviceContainer,
            IConfigurationValueProvider configurationValueProvider)
        {
            _parentContainer = parentContainer;
            _logger = logger;
            _serviceContainer = serviceContainer;
            _configurationValueProvider = configurationValueProvider;

            ActivationGroups = new HashSet<string>();
            ServiceProviders = new Dictionary<Type, IServiceProvider>();
            OverrideTypes = new HashSet<Type>();
        }

        public IDictionary<Type, IServiceProvider> ServiceProviders { get; }
        public ICollection<string> ActivationGroups { get; }
        public ICollection<Type> OverrideTypes { get; }

        protected override async Task SynchronizedStartAsync(CancellationToken cancellationToken)
        {
            if (ActivationGroups == null ||
                ActivationGroups.Count == 0)
            {
                throw new InvalidOperationException($"There's no defined activation group names. Please add at least one it into '{nameof(ActivationGroups)}' property.");
            }

            _logger.Debug("Loading types for activation...");

            foreach (var serviceType in GetServiceTypes())
            {
                var activatable = GetActivatable(serviceType);
                if (!ActivationGroups.Contains(activatable.Group)) continue;

                _logger.Write(LogEventLevel.Debug, nameof(ServiceActivator), null,
                    "Setting up service '{ServiceType}' for activation...", serviceType.Name);

                try
                {
                    var container = CreateServiceContainer(serviceType, activatable);
                    var service = container.GetInstance<IService>();

                    _serviceContainer.Add(service, activatable.Tier);
                    ServiceProviders.Add(serviceType, container);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred while activating service '{ServiceType}'", serviceType.Name);

                    throw;
                }
            }

            _logger.Debug("Starting service container...");

            await _serviceContainer.StartAsync(cancellationToken);

            _logger.Debug("Service container started");

            await base.SynchronizedStartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken) =>
            _serviceContainer.Execution;

        protected override void SynchronizedStop()
        {
            _serviceContainer.Stop();
            base.SynchronizedStop();
        }

        protected virtual IEnumerable<Type> GetServiceTypes()
        {
            // Call LIME's TypeUtil since it loads all the referenced assemblies.     
            var @namespace = "";
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var searchPattern = $"{@namespace}*.dll";

            ReferencesUtil.LoadAssembliesAndReferences(
                path,
                searchPattern,
                a => a.FullName.StartsWith(@namespace));

            return ReferencesUtil
                .GetAllLoadedTypes()
                .Where(
                    t => t.IsClass &&
                         !t.IsAbstract &&
                         t.GetCustomAttribute<ActivateAttribute>() != null &&
                         typeof(IService).IsAssignableFrom(t));
        }

        protected virtual IActivatable GetActivatable(Type serviceType)
        {
            return serviceType.GetCustomAttribute<ActivateAttribute>();
        }

        protected virtual Container CreateServiceContainer(Type serviceType, IActivatable activatable)
        {
            var container = new Container();
            container.Options.AllowOverridingRegistrations = true;
            container.Options.SuppressLifestyleMismatchVerification = true;
            container.ResolveUnregisteredType += LinkToParentContainer;

            if (activatable.RegistrableType != null &&
                typeof(IRegistrable).IsAssignableFrom(activatable.RegistrableType))
            {
                var registrable = (IRegistrable)_parentContainer.GetInstance(activatable.RegistrableType);
                registrable.RegisterTo(container);
            }
            else
            {
                container.RegisterSingleton(typeof(IService), serviceType);
            }

            if (!activatable.PreserveDiagnostics)
            {
                container.RegisterInstance(_logger);
            }

            if (!string.IsNullOrWhiteSpace(activatable.ConfigurationValuePrefix))
            {
                var prefixConfigurationValueProvider = new PrefixConfigurationValueProvider(
                    _configurationValueProvider,
                    activatable.ConfigurationValuePrefix);

                container.RegisterInstance<IConfigurationValueProvider>(prefixConfigurationValueProvider);
            }

            foreach (var overrideType in OverrideTypes)
            {
                container.RegisterSingleton(
                    overrideType,
                    () => _parentContainer.GetInstance(overrideType));
            }

            return container;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var serviceProvidersValue in ServiceProviders.Values.ToList())
                {
                    serviceProvidersValue.DisposeIfDisposable();
                }
            }
        }

        private void LinkToParentContainer(object sender, UnregisteredTypeEventArgs args)
        {
            if (!args.Handled)
            {
                args.Register(() => _parentContainer.GetInstance(args.UnregisteredServiceType));
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}