using System;
using System.Collections.Generic;
using System.Text;

namespace Take.Hosting
{
    internal static class DisposableExtensions
    {
        public static void DisposeIfDisposable(this object candidate) => (candidate as IDisposable)?.Dispose();
    }
}
