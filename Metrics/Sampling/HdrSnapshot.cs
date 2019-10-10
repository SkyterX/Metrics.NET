using System.Collections.Generic;
using System.Linq;

using HdrHistogram;

namespace Metrics.Sampling
{
    internal sealed class HdrSnapshot : Snapshot
    {
        public HdrSnapshot(AbstractHistogram histogram, long minValue, string minUserValue, long maxValue, string maxUserValue)
        {
            this.histogram = histogram;
            Min = minValue;
            MinUserValue = minUserValue;
            Max = maxValue;
            MaxUserValue = maxUserValue;
        }

        public IEnumerable<long> Values { get { return histogram.RecordedValues().Select(v => v.getValueIteratedTo()); } }

        public double GetValue(double quantile)
        {
            return histogram.getValueAtPercentile(quantile * 100);
        }

        public long Min { get; }
        public string MinUserValue { get; }
        public long Max { get; }
        public string MaxUserValue { get; }

        public long Count => histogram.getTotalCount();
        public double Mean => histogram.getMean();
        public double StdDev => histogram.getStdDeviation();

        public double Median => histogram.getValueAtPercentile(50);
        public double Percentile75 => histogram.getValueAtPercentile(75);
        public double Percentile95 => histogram.getValueAtPercentile(95);
        public double Percentile98 => histogram.getValueAtPercentile(98);
        public double Percentile99 => histogram.getValueAtPercentile(99);
        public double Percentile999 => histogram.getValueAtPercentile(99.9);

        public int Size => histogram.getEstimatedFootprintInBytes();
        private readonly AbstractHistogram histogram;
    }
}