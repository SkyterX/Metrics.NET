using System;

using Metrics.ConcurrencyUtilities;
using Metrics.MetricData;
using Metrics.Sampling;
using Metrics.Utils;

namespace Metrics.Core
{
    public interface TimerImplementation : Timer, MetricValueProvider<TimerValue>
    {
    }

    public sealed class TimerMetric : TimerImplementation, IDisposable
    {
        public TimerMetric()
            : this(new HistogramMetric(), new MeterMetric(), Clock.Default)
        {
        }

        public TimerMetric(SamplingType samplingType)
            : this(new HistogramMetric(samplingType), new MeterMetric(), Clock.Default)
        {
        }

        public TimerMetric(HistogramImplementation histogram)
            : this(histogram, new MeterMetric(), Clock.Default)
        {
        }

        public TimerMetric(Reservoir reservoir)
            : this(new HistogramMetric(reservoir), new MeterMetric(), Clock.Default)
        {
        }

        public TimerMetric(SamplingType samplingType, MeterImplementation meter, Clock clock)
            : this(new HistogramMetric(samplingType), meter, clock)
        {
        }

        public TimerMetric(HistogramImplementation histogram, MeterImplementation meter, Clock clock)
        {
            this.clock = clock;
            this.meter = meter;
            this.histogram = histogram;
        }

        public void Record(long duration, TimeUnit unit, string userValue = null)
        {
            var nanos = unit.ToNanoseconds(duration);
            if (nanos >= 0)
            {
                histogram.Update(nanos, userValue);
                totalRecordedTime.Add(nanos);
                // Do not save user value in meter, because its only purpose is to provide overall rate metrics.
                // Unlike meter, user values for histogram are not restricted to come from a finite set, and generally they are unique.
                // Saving them will cause huge number of sub-meter-per-item allocations inside a meter metric. 
                // Moreover those sub-meters are useless, since rates-per-item are not reported for timers.
                meter.Mark();
            }
        }

        public void Time(Action action, string userValue = null)
        {
            var start = clock.Nanoseconds;
            try
            {
                activeSessionsCounter.Increment();
                action();
            }
            finally
            {
                activeSessionsCounter.Decrement();
                Record(clock.Nanoseconds - start, TimeUnit.Nanoseconds, userValue);
            }
        }

        public T Time<T>(Func<T> action, string userValue = null)
        {
            var start = clock.Nanoseconds;
            try
            {
                activeSessionsCounter.Increment();
                return action();
            }
            finally
            {
                activeSessionsCounter.Decrement();
                Record(clock.Nanoseconds - start, TimeUnit.Nanoseconds, userValue);
            }
        }

        public long StartRecording()
        {
            activeSessionsCounter.Increment();
            return clock.Nanoseconds;
        }

        public long CurrentTime()
        {
            return clock.Nanoseconds;
        }

        public long EndRecording()
        {
            activeSessionsCounter.Decrement();
            return clock.Nanoseconds;
        }

        public TimerContext NewContext(string userValue = null)
        {
            return new TimerContext(this, userValue);
        }

        public TimerValue Value { get { return GetValue(); } }

        public TimerValue GetValue(bool resetMetric = false)
        {
            var total = resetMetric ? totalRecordedTime.GetAndReset() : totalRecordedTime.GetValue();
            return new TimerValue(meter.GetValue(resetMetric), histogram.GetValue(resetMetric), activeSessionsCounter.GetValue(), total, TimeUnit.Nanoseconds);
        }

        public void Reset()
        {
            meter.Reset();
            histogram.Reset();
        }

        public void Dispose()
        {
            using (histogram as IDisposable)
            {
            }
            using (meter as IDisposable)
            {
            }
        }

        private readonly Clock clock;
        private readonly MeterImplementation meter;
        private readonly HistogramImplementation histogram;
        private readonly StripedLongAdder activeSessionsCounter = new StripedLongAdder();
        private readonly StripedLongAdder totalRecordedTime = new StripedLongAdder();
    }
}