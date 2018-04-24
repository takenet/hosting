using System;
using System.Threading;
using System.Threading.Tasks;

namespace Take.Hosting
{
    /// <summary>
    /// Base class for building services.
    /// Provide methods for synchronization for the start and stop operations.
    /// </summary>
    public abstract class ServiceBase : IService
    {
        private readonly SemaphoreSlim _semaphore;
        private CancellationTokenSource _cts;

        protected ServiceBase()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        public Task Execution { get; protected set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Execution != null)
                {
                    throw new InvalidOperationException("The service is already started");
                }
                await SynchronizedStartAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Stop()
        {
            _semaphore.Wait();

            try
            {
                if (Execution == null)
                {
                    throw new InvalidOperationException("The service is not started");
                }
                SynchronizedStop();
            }
            finally
            {
                _semaphore.Release();
            }
        }


        protected virtual Task SynchronizedStartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _cts = new CancellationTokenSource();
            Execution = ExecuteImplAsync(_cts.Token);
            return Task.CompletedTask;
        }

        protected virtual void SynchronizedStop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        private async Task ExecuteImplAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }
    }
}