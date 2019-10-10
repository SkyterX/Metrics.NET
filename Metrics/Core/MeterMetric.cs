using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using Metrics.MetricData;
using Metrics.Utils;

namespace Metrics.Core
{
    public interface MeterImplementation : Meter, MetricValueProvider<MeterValue>
    {
    }

    public sealed class MeterMetric : SimpleMeter, MeterImplementation, IDisposable
    {
        public MeterMetric()
            : this(Clock.Default, new ActionScheduler())
        {
        }

        public MeterMetric(Clock clock, Scheduler scheduler)
        {
            this.clock = clock;
            startTime = this.clock.Nanoseconds;
            tickScheduler = scheduler;
            tickScheduler.Start(TickInterval, (Action)Tick);
        }

        public MeterValue Value { get { return GetValue(); } }

        public void Mark()
        {
            Mark(1L);
        }

        public new void Mark(long count)
        {
            base.Mark(count);
        }

        public void Mark(string item)
        {
            Mark(item, 1L);
        }

        public void Mark(string item, long count)
        {
            Mark(count);

            if (item == null)
            {
                return;
            }

            if (setMeters == null)
            {
                Interlocked.CompareExchange(ref setMeters, new ConcurrentDictionary<string, SimpleMeter>(), null);
            }

            Debug.Assert(setMeters != null);
            setMeters.GetOrAdd(item, v => new SimpleMeter()).Mark(count);
        }

        public MeterValue GetValue(bool resetMetric = false)
        {
            if (setMeters == null || setMeters.Count == 0)
            {
                double elapsed = (clock.Nanoseconds - startTime);
                var value = base.GetValue(elapsed);
                if (resetMetric)
                {
                    Reset();
                }
                return value;
            }

            return GetValueWithSetItems(resetMetric);
        }

        private MeterValue GetValueWithSetItems(bool resetMetric)
        {
            double elapsed = clock.Nanoseconds - startTime;
            var value = base.GetValue(elapsed);

            Debug.Assert(setMeters != null);

            var items = new MeterValue.SetItem[setMeters.Count];
            var index = 0;

            foreach (var meter in setMeters)
            {
                var itemValue = meter.Value.GetValue(elapsed);
                var percent = value.Count > 0 ? itemValue.Count / (double)value.Count * 100 : 0.0;
                items[index++] = new MeterValue.SetItem(meter.Key, percent, itemValue);
                if (index == items.Length)
                {
                    break;
                }
            }

            Array.Sort(items, MeterValue.SetItemComparer);
            var result = new MeterValue(value.Count, value.MeanRate, value.OneMinuteRate, value.FiveMinuteRate, value.FifteenMinuteRate, TimeUnit.Seconds, items);
            if (resetMetric)
            {
                Reset();
            }
            return result;
        }

        private new void Tick()
        {
            base.Tick();
            if (setMeters != null)
            {
                foreach (var value in setMeters.Values)
                {
                    value.Tick();
                }
            }
        }

        public void Dispose()
        {
            tickScheduler.Stop();
            using (tickScheduler)
            {
            }

            if (setMeters != null)
            {
                setMeters.Clear();
                setMeters = null;
            }
        }

        public new void Reset()
        {
            startTime = clock.Nanoseconds;
            base.Reset();
            if (setMeters != null)
            {
                foreach (var meter in setMeters.Values)
                {
                    meter.Reset();
                }
            }
        }

        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

        private ConcurrentDictionary<string, SimpleMeter> setMeters;

        private readonly Clock clock;
        private readonly Scheduler tickScheduler;

        private long startTime;
    }
}