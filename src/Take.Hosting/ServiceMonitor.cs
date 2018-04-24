using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace Take.Hosting
{
    public sealed class ServiceMonitor : IServiceMonitor
    {
        private readonly ILogger _logger;

        public ServiceMonitor(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable Monitor(IService service, TimeSpan delay)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (service.Execution == null) throw new ArgumentException("The service execution task is not defined");
            var cts = new CancellationTokenSource();
            return MonitorImpl(service, delay, cts);
        }

        private IDisposable MonitorImpl(IService service, TimeSpan delay, CancellationTokenSource cts)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (cts == null) throw new ArgumentNullException(nameof(cts));
            if (!cts.IsCancellationRequested)
            {
                service.Execution.ContinueWith(async t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.Error(t.Exception,
                            "The service '{ServiceType}' is faulted and will be restarted",
                            service.GetType().Name);
                    }

                    try
                    {
                        if (delay > TimeSpan.Zero) await Task.Delay(delay, cts.Token);
                        await service.StartAsync(cts.Token);
                        MonitorImpl(service, delay, cts);
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
                    catch (Exception ex)
                    {
                        _logger.Error(ex,
                            "The service '{ServiceType}' could not be restarted",
                            service.GetType().Name
                            );
                        throw;
                    }

                },
                cts.Token,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.NotOnCanceled,
                TaskScheduler.Default);
            }
            return cts;
        }
    }
}
