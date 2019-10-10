using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metrics
{
    internal static class MetricsErrorHandler
    {
        static MetricsErrorHandler()
        {
            AddHandler((e, msg) => Trace.TraceError($"Metrics.NET: {msg} {e}"));
        }

        public static void AddHandler(Action<Exception, string> handler)
        {
            handlers.Add(handler);
        }

        public static void ClearHandlers()
        {
            while (!handlers.IsEmpty)
                handlers.TryTake(out _);
        }

        public static void Handle(Exception exception, string message)
        {
            errorMeter.Mark();

            foreach (var handler in handlers)
            {
                try
                {
                    handler(exception, message);
                }
                catch
                {
                    // error handler throw-ed on us, hope you have a debugger attached.
                }
            }
        }

        private static readonly Meter errorMeter = Metric.Internal.Meter("Metrics Errors", Unit.Errors);
        private static readonly ConcurrentBag<Action<Exception, string>> handlers = new ConcurrentBag<Action<Exception, string>>();
    }
}