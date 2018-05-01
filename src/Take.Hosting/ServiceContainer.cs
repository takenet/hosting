using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace Take.Hosting
{
    public sealed class ServiceContainer : IServiceContainer
    {
        private readonly IServiceMonitor _serviceMonitor;
        private readonly ILogger _logger;
        private readonly Dictionary<int, List<IService>> _tierServicesDictionary;
        private readonly ConcurrentBag<IDisposable> _serviceMonitors;
        private readonly SemaphoreSlim _initializationSemaphore;

        private SemaphoreSlim _executionSemaphore;

        private static readonly TimeSpan ServiceStopTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MonitorDelay = TimeSpan.FromSeconds(1);

        public ServiceContainer(IServiceMonitor serviceMonitor, ILogger logger)
        {
            _serviceMonitor = serviceMonitor;
            _logger = logger;
            _tierServicesDictionary = new Dictionary<int, List<IService>>();
            _serviceMonitors = new ConcurrentBag<IDisposable>();
            _initializationSemaphore = new SemaphoreSlim(1);
        }

        public Task Execution { get; private set; }

        public IServiceContainer Add(IService service, int tier = 0)
        {
            _initializationSemaphore.Wait();
            try
            {
                if (_executionSemaphore != null)
                {
                    throw new InvalidOperationException("Could not add a service after the hosting service was started");
                }

                if (service == null) throw new ArgumentNullException(nameof(service));
                if (service.Execution != null)
                {
                    throw new ArgumentException($"The service '{service.GetType().Name}' execution task is already defined", nameof(service));
                }

                if (!_tierServicesDictionary.TryGetValue(tier, out var services))
                {
                    services = new List<IService>();
                    _tierServicesDictionary.Add(tier, services);
                }

                services.Add(service);
            }
            finally
            {
                _initializationSemaphore.Release();
            }

            return this;
        }

        public int Count => _tierServicesDictionary.Values.Select(v => v.Count).Sum();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _initializationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_executionSemaphore != null)
                {
                    throw new InvalidOperationException("The hosting service was already started");
                }

                if (_tierServicesDictionary.Count == 0)
                {
                    throw new InvalidOperationException("The hosting service list is empty");
                }

                // Start the services starting by the lowest tier
                foreach (var tier in _tierServicesDictionary.Keys.OrderBy(t => t))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tierServices = _tierServicesDictionary[tier];

                    // Start the tier services in parallel
                    var tierStartTasks =
                        tierServices
                            .Select(s => Task.Run(async () =>
                            {
                                _logger.Debug("Starting service '{ServiceName}' on tier {Tier}...", s.GetType().Name, tier);

                                await s.StartAsync(cancellationToken);

                                _serviceMonitors.Add(
                                    _serviceMonitor.Monitor(s, MonitorDelay));

                                _logger.Debug("Service '{ServiceType}' started", s.GetType().Name);
                            }, cancellationToken));

                    await Task.WhenAll(tierStartTasks).ConfigureAwait(false);
                }

                // Orchestrate the shutdown of the services tasks
                _executionSemaphore = new SemaphoreSlim(0, 1);
                Execution = ExecuteAsync();
            }
            catch
            {
                DisposeMonitors();
                throw;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        public void Stop()
        {
            _initializationSemaphore.Wait();
            try
            {
                if (_executionSemaphore == null)
                {
                    throw new InvalidOperationException("The hosting service is not started");
                }

                if (_executionSemaphore.CurrentCount > 0)
                {
                    throw new InvalidOperationException("The hosting service is already stopping");
                }

                DisposeMonitors();
                _executionSemaphore.Release();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task ExecuteAsync()
        {
            // Await for the stop signal
            await _executionSemaphore.WaitAsync().ConfigureAwait(false);

            // Stop the services of each tier
            var exceptions = new List<Exception>();

            foreach (var tier in _tierServicesDictionary.Keys.OrderByDescending(t => t))
            {
                var tierServices = _tierServicesDictionary[tier];

                var tierExecutionTasks = new List<Task>();
                foreach (var tierService in tierServices)
                {
                    try
                    {
                        _logger.Debug("Stopping service '{ServiceType}' on tier {Tier}...", tierService.GetType().Name, tier);

                        tierService.Stop();

                        var tierServiceExecutionTask = tierService.Execution;
                        if (tierServiceExecutionTask == null)
                        {
                            exceptions.Add(
                                new Exception($"The service {tierService.GetType().Name} has an invalid execution task"));
                        }
                        else
                        {
                            var cts = new CancellationTokenSource(ServiceStopTimeout);

                            tierExecutionTasks.Add(
                                tierServiceExecutionTask
                                    .WithCancellation(cts.Token)
                                    .ContinueWith(async t =>
                                    {
                                        if (t.IsFaulted)
                                        {
                                            _logger.Error(t.Exception,
                                                "Service '{ServiceType}' stopped in faulted state on tier {Tier}", tierService.GetType().Name, tier);
                                        }
                                        else
                                        {
                                            _logger.Debug("Service '{ServiceType}' stopped successfully on tier {Tier}", tierService.GetType().Name, tier);
                                        }

                                        cts.Dispose();

                                        return t;
                                    })
                                    .Unwrap());
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "One or more services failed to stop on tier {Tier}", tier);

                        exceptions.Add(ex);
                    }
                }

                if (tierExecutionTasks.Any())
                {
                    var tierExecutionTask = Task.WhenAll(tierExecutionTasks);
                    try
                    {
                        _logger.Debug("Awaiting {TaskCount} execution tasks on tier {Tier}...", tierExecutionTasks.Count, tier);
                        await tierExecutionTask.ConfigureAwait(false);
                        _logger.Debug("Execution tasks finished on tier {Tier}", tier);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Execution tasks failed on tier {Tier}", tier);
                        exceptions.Add(tierExecutionTask.Exception ?? ex);
                    }
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }

            _executionSemaphore = null;
        }

        private void DisposeMonitors()
        {
            IDisposable monitor;
            while (_serviceMonitors.TryTake(out monitor)) monitor.Dispose();
        }
    }
}