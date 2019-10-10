using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Metrics.MetricData;

namespace Metrics.Core
{
    public sealed class DefaultMetricsRegistry : MetricsRegistry
    {
        public DefaultMetricsRegistry()
        {
            DataProvider = new DefaultRegistryDataProvider(() => gauges.All, () => counters.All, () => meters.All, () => histograms.All, () => timers.All);
        }

        public RegistryDataProvider DataProvider { get; }

        public void Gauge(string name, Func<MetricValueProvider<double>> valueProvider, Unit unit, MetricTags tags)
        {
            gauges.GetOrAdd(name, () =>
                {
                    MetricValueProvider<double> gauge = valueProvider();
                    return Tuple.Create(gauge, new GaugeValueSource(name, gauge, unit, tags));
                });
        }

        public Counter Counter<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : CounterImplementation
        {
            return counters.GetOrAdd(name, () =>
                {
                    T counter = builder();
                    return Tuple.Create((Counter)counter, new CounterValueSource(name, counter, unit, tags));
                });
        }

        public Meter Meter<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, MetricTags tags)
            where T : MeterImplementation
        {
            return meters.GetOrAdd(name, () =>
                {
                    T meter = builder();
                    return Tuple.Create((Meter)meter, new MeterValueSource(name, meter, unit, rateUnit, tags));
                });
        }

        public Histogram Histogram<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : HistogramImplementation
        {
            return histograms.GetOrAdd(name, () =>
                {
                    T histogram = builder();
                    return Tuple.Create((Histogram)histogram, new HistogramValueSource(name, histogram, unit, tags));
                });
        }

        public Timer Timer<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
            where T : TimerImplementation
        {
            return timers.GetOrAdd(name, () =>
                {
                    T timer = builder();
                    return Tuple.Create((Timer)timer, new TimerValueSource(name, timer, unit, rateUnit, durationUnit, tags));
                });
        }

        public void ClearAllMetrics()
        {
            gauges.Clear();
            counters.Clear();
            meters.Clear();
            histograms.Clear();
            timers.Clear();
        }

        public void ResetMetricsValues()
        {
            gauges.Reset();
            counters.Reset();
            meters.Reset();
            histograms.Reset();
            timers.Reset();
        }

        private readonly MetricMetaCatalog<MetricValueProvider<double>, GaugeValueSource, double> gauges = new MetricMetaCatalog<MetricValueProvider<double>, GaugeValueSource, double>();
        private readonly MetricMetaCatalog<Counter, CounterValueSource, CounterValue> counters = new MetricMetaCatalog<Counter, CounterValueSource, CounterValue>();
        private readonly MetricMetaCatalog<Meter, MeterValueSource, MeterValue> meters = new MetricMetaCatalog<Meter, MeterValueSource, MeterValue>();

        private readonly MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue> histograms =
            new MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue>();

        private readonly MetricMetaCatalog<Timer, TimerValueSource, TimerValue> timers = new MetricMetaCatalog<Timer, TimerValueSource, TimerValue>();

        private class MetricMetaCatalog<TMetric, TValue, TMetricValue>
            where TValue : MetricValueSource<TMetricValue>
        {
            public IEnumerable<TValue> All { get { return metrics.Values.OrderBy(m => m.Name).Select(v => v.Value); } }

            public TMetric GetOrAdd(string name, Func<Tuple<TMetric, TValue>> metricProvider)
            {
                return metrics.GetOrAdd(name, n =>
                    {
                        var result = metricProvider();
                        return new MetricMeta(result.Item1, result.Item2);
                    }).Metric;
            }

            public void Clear()
            {
                var values = metrics.Values;
                metrics.Clear();
                foreach (var value in values)
                {
                    using (value.Metric as IDisposable)
                    {
                    }
                }
            }

            public void Reset()
            {
                foreach (var metric in metrics.Values)
                {
                    var resetable = metric.Metric as ResetableMetric;
                    resetable?.Reset();
                }
            }

            private readonly ConcurrentDictionary<string, MetricMeta> metrics =
                new ConcurrentDictionary<string, MetricMeta>();

            public class MetricMeta
            {
                public MetricMeta(TMetric metric, TValue valueUnit)
                {
                    Metric = metric;
                    Value = valueUnit;
                }

                public string Name => Value.Name;
                public TMetric Metric { get; }
                public TValue Value { get; }
            }
        }
    }
}