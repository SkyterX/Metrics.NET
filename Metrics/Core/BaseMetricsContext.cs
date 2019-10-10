using System;
using System.Collections.Concurrent;
using System.Linq;

using Metrics.MetricData;
using Metrics.Sampling;

namespace Metrics.Core
{
    public abstract class BaseMetricsContext : MetricsContext, AdvancedMetricsContext
    {
        protected BaseMetricsContext(string context, MetricsRegistry registry, MetricsBuilder metricsBuilder, Func<DateTime> timestampProvider)
        {
            this.registry = registry;
            this.metricsBuilder = metricsBuilder;
            DataProvider = new DefaultDataProvider(context, timestampProvider, this.registry.DataProvider, () => childContexts.Values.Select(c => c.DataProvider));
        }

        protected abstract MetricsContext CreateChildContextInstance(string contextName);

        public AdvancedMetricsContext Advanced => this;

        public event EventHandler ContextShuttingDown;
        public event EventHandler ContextDisabled;

        public MetricsDataProvider DataProvider { get; }

        public MetricsContext Context(string contextName)
        {
            return Context(contextName, c => CreateChildContextInstance(contextName));
        }

        public MetricsContext Context(string contextName, Func<string, MetricsContext> contextCreator)
        {
            if (isDisabled)
            {
                return this;
            }

            if (string.IsNullOrEmpty(contextName))
            {
                return this;
            }

            return childContexts.GetOrAdd(contextName, contextCreator);
        }

        public bool AttachContext(string contextName, MetricsContext context)
        {
            if (isDisabled)
            {
                return true;
            }

            if (string.IsNullOrEmpty(contextName))
            {
                throw new ArgumentException("Context name can't be null or empty for attached contexts");
            }
            var attached = childContexts.GetOrAdd(contextName, context);
            return ReferenceEquals(attached, context);
        }

        public void ShutdownContext(string contextName)
        {
            if (string.IsNullOrEmpty(contextName))
            {
                throw new ArgumentException("contextName must not be null or empty", contextName);
            }

            MetricsContext context;
            if (childContexts.TryRemove(contextName, out context))
            {
                using (context)
                {
                }
            }
        }

        public void PerformanceCounter(string name, string counterCategory, string counterName, string counterInstance, Unit unit, MetricTags tags)
        {
            Gauge(name, () => metricsBuilder.BuildPerformanceCounter(name, unit, counterCategory, counterName, counterInstance), unit, tags);
        }

        public void Gauge(string name, Func<double> valueProvider, Unit unit, MetricTags tags)
        {
            Gauge(name, () => metricsBuilder.BuildGauge(name, unit, valueProvider), unit, tags);
        }

        public void Gauge(string name, Func<MetricValueProvider<double>> valueProvider, Unit unit, MetricTags tags)
        {
            registry.Gauge(name, valueProvider, unit, tags);
        }

        public Counter Counter(string name, Unit unit, MetricTags tags)
        {
            return Counter(name, unit, () => metricsBuilder.BuildCounter(name, unit), tags);
        }

        public Counter Counter<T>(string name, Unit unit, Func<T> builder, MetricTags tags)
            where T : CounterImplementation
        {
            return registry.Counter(name, builder, unit, tags);
        }

        public Meter Meter(string name, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            return Meter(name, unit, () => metricsBuilder.BuildMeter(name, unit, rateUnit), rateUnit, tags);
        }

        public Meter Meter<T>(string name, Unit unit, Func<T> builder, TimeUnit rateUnit, MetricTags tags)
            where T : MeterImplementation
        {
            return registry.Meter(name, builder, unit, rateUnit, tags);
        }

        public Histogram Histogram(string name, Unit unit, SamplingType samplingType, MetricTags tags)
        {
            return Histogram(name, unit, () => metricsBuilder.BuildHistogram(name, unit, samplingType), tags);
        }

        public Histogram Histogram<T>(string name, Unit unit, Func<T> builder, MetricTags tags)
            where T : HistogramImplementation
        {
            return registry.Histogram(name, builder, unit, tags);
        }

        public Histogram Histogram(string name, Unit unit, Func<Reservoir> builder, MetricTags tags)
        {
            return Histogram(name, unit, () => metricsBuilder.BuildHistogram(name, unit, builder()), tags);
        }

        public Timer Timer(string name, Unit unit, SamplingType samplingType, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return registry.Timer(name, () => metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, samplingType), unit, rateUnit, durationUnit, tags);
        }

        public Timer Timer<T>(string name, Unit unit, Func<T> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
            where T : TimerImplementation
        {
            return registry.Timer(name, builder, unit, rateUnit, durationUnit, tags);
        }

        public Timer Timer(string name, Unit unit, Func<HistogramImplementation> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return Timer(name, unit, () => metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, builder()), rateUnit, durationUnit, tags);
        }

        public Timer Timer(string name, Unit unit, Func<Reservoir> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return Timer(name, unit, () => metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, builder()), rateUnit, durationUnit, tags);
        }

        public void CompletelyDisableMetrics()
        {
            if (isDisabled)
            {
                return;
            }

            isDisabled = true;

            var oldRegistry = registry;
            registry = new NullMetricsRegistry();
            oldRegistry.ClearAllMetrics();
            using (oldRegistry as IDisposable)
            {
            }

            ForAllChildContexts(c => c.Advanced.CompletelyDisableMetrics());

            ContextShuttingDown?.Invoke(this, EventArgs.Empty);
            ContextDisabled?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!isDisabled)
                {
                    ContextShuttingDown?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void ResetMetricsValues()
        {
            registry.ResetMetricsValues();
            ForAllChildContexts(c => c.Advanced.ResetMetricsValues());
        }

        public void WithCustomMetricsBuilder(MetricsBuilder metricsBuilder)
        {
            this.metricsBuilder = metricsBuilder;
            ForAllChildContexts(c => c.Advanced.WithCustomMetricsBuilder(metricsBuilder));
        }

        private void ForAllChildContexts(Action<MetricsContext> action)
        {
            foreach (var context in childContexts.Values)
            {
                action(context);
            }
        }

        private readonly ConcurrentDictionary<string, MetricsContext> childContexts = new ConcurrentDictionary<string, MetricsContext>();

        private MetricsRegistry registry;
        private MetricsBuilder metricsBuilder;

        private bool isDisabled;
    }
}