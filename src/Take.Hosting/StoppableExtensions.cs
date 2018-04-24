namespace Take.Hosting
{
    public static class StoppableExtensions
    {
        /// <summary>
        /// Calls the Stop method if the object is an <see cref="IStoppable"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        public static void StopIfStoppable<T>(this T value)
        {
            (value as IStoppable)?.Stop();
        }
    }
}
