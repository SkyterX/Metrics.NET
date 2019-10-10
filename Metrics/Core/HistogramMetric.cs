using System;

using Metrics.MetricData;
using Metrics.Sampling;

namespace Metrics.Core
{
    public interface HistogramImplementation : Histogram, MetricValueProvider<HistogramValue>
    {
    }

    public sealed class HistogramMetric : HistogramImplementation
    {
        public HistogramMetric()
            : this(SamplingType.Default)
        {
        }

        public HistogramMetric(SamplingType samplingType)
            : this(SamplingTypeToReservoir(samplingType))
        {
        }

        public HistogramMetric(Reservoir reservoir)
        {
            this.reservoir = reservoir;
        }

        public void Update(long value, string userValue = null)
        {
            last = new UserValueWrapper(value, userValue);
            reservoir.Update(value, userValue);
        }

        public HistogramValue GetValue(bool resetMetric = false)
        {
            var value = new HistogramValue(last.Value, last.UserValue, reservoir.GetSnapshot(resetMetric));
            if (resetMetric)
            {
                last = UserValueWrapper.Empty;
            }
            return value;
        }

        public HistogramValue Value { get { return GetValue(); } }

        public void Reset()
        {
            last = UserValueWrapper.Empty;
            reservoir.Reset();
        }

        private static Reservoir SamplingTypeToReservoir(SamplingType samplingType)
        {
            while (true)
            {
                switch (samplingType)
                {
                case SamplingType.Default:
                    samplingType = Metric.Config.DefaultSamplingType;
                    continue;
                case SamplingType.HighDynamicRange:
                    return new HdrHistogramReservoir();
                case SamplingType.ExponentiallyDecaying:
                    return new ExponentiallyDecayingReservoir();
                case SamplingType.LongTerm:
                    return new UniformReservoir();
                case SamplingType.SlidingWindow:
                    return new SlidingWindowReservoir();
                }
                throw new InvalidOperationException("Sampling type not implemented " + samplingType);
            }
        }

        private readonly Reservoir reservoir;
        private UserValueWrapper last;
    }
}