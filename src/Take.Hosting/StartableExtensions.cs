using System.Threading;
using System.Threading.Tasks;

namespace Take.Hosting
{
    public static class StartableExtensions
    {
        /// <summary>
        /// Calls the StartAsync method if the object is an <see cref="IStartable"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task StartIfStartableAsync<T>(this T value, CancellationToken cancellationToken)
        {
            var startable = value as IStartable;
            return startable != null ? startable.StartAsync(cancellationToken) : Task.CompletedTask;
        }
    }
}