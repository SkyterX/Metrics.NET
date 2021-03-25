using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metrics
{
    internal static class MetricsErrorHandler
    {
        static MetricsErrorHandler()
        {
            AddHandler((e, msg, args) => Trace.TraceError($"Metrics.NET: {string.Format(msg, args)} {e}"));
        }

        public static void AddHandler(Action<Exception, string, object[]> handler)
        {
            handlers.Add(handler);
        }

        public static void ClearHandlers()
        {
            while (!handlers.IsEmpty)
                handlers.TryTake(out _);
        }

        public static void Handle(Exception exception, string messageTemplate, params object[] templateArgs)
        {
            errorMeter.Mark();

            foreach (var handler in handlers)
            {
                try
                {
                    handler(exception, messageTemplate, templateArgs);
                }
                catch
                {
                    // error handler throw-ed on us, hope you have a debugger attached.
                }
            }
        }

        private static readonly Meter errorMeter = Metric.Internal.Meter("Metrics Errors", Unit.Errors);
        private static readonly ConcurrentBag<Action<Exception, string, object[]>> handlers = new ConcurrentBag<Action<Exception, string, object[]>>();
    }
}