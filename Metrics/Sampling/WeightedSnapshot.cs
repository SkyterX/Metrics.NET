using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics.Sampling
{
    public struct WeightedSample
    {
        public WeightedSample(long value, string userValue, double weight)
        {
            Value = value;
            UserValue = userValue;
            Weight = weight;
        }

        public readonly long Value;
        public readonly string UserValue;
        public readonly double Weight;
    }

    public sealed class WeightedSnapshot : Snapshot
    {
        public WeightedSnapshot(long count, IEnumerable<WeightedSample> values)
        {
            Count = count;
            var sample = values.ToArray();
            Array.Sort(sample, WeightedSampleComparer.Instance);

            var sumWeight = sample.Sum(s => s.Weight);

            this.values = new long[sample.Length];
            normWeights = new double[sample.Length];
            quantiles = new double[sample.Length];

            for (var i = 0; i < sample.Length; i++)
            {
                this.values[i] = sample[i].Value;
                normWeights[i] = sample[i].Weight / sumWeight;
                if (i > 0)
                {
                    quantiles[i] = quantiles[i - 1] + normWeights[i - 1];
                }
            }

            MinUserValue = sample.Select(s => s.UserValue).FirstOrDefault();
            MaxUserValue = sample.Select(s => s.UserValue).LastOrDefault();
        }

        public long Count { get; }
        public int Size => values.Length;

        public long Max => values.LastOrDefault();
        public long Min => values.FirstOrDefault();

        public string MaxUserValue { get; }
        public string MinUserValue { get; }

        public double Mean
        {
            get
            {
                if (values.Length == 0)
                {
                    return 0.0;
                }

                double sum = 0;
                for (var i = 0; i < values.Length; i++)
                {
                    sum += values[i] * normWeights[i];
                }
                return sum;
            }
        }

        public double StdDev
        {
            get
            {
                if (Size <= 1)
                {
                    return 0;
                }

                var mean = Mean;
                double variance = 0;

                for (var i = 0; i < values.Length; i++)
                {
                    var diff = values[i] - mean;
                    variance += normWeights[i] * diff * diff;
                }

                return Math.Sqrt(variance);
            }
        }

        public double Median => GetValue(0.5d);
        public double Percentile75 => GetValue(0.75d);
        public double Percentile95 => GetValue(0.95d);
        public double Percentile98 => GetValue(0.98d);
        public double Percentile99 => GetValue(0.99d);
        public double Percentile999 => GetValue(0.999d);

        public IEnumerable<long> Values => values;

        public double GetValue(double quantile)
        {
            if (quantile < 0.0 || quantile > 1.0 || double.IsNaN(quantile))
            {
                throw new ArgumentException($"{quantile} is not in [0..1]");
            }

            if (Size == 0)
            {
                return 0;
            }

            var posx = Array.BinarySearch(quantiles, quantile);
            if (posx < 0)
            {
                posx = ~posx - 1;
            }

            if (posx < 1)
            {
                return values[0];
            }

            return posx >= values.Length ? values[values.Length - 1] : values[posx];
        }

        private readonly long[] values;
        private readonly double[] normWeights;
        private readonly double[] quantiles;

        private class WeightedSampleComparer : IComparer<WeightedSample>
        {
            public int Compare(WeightedSample x, WeightedSample y)
            {
                return Comparer<long>.Default.Compare(x.Value, y.Value);
            }

            public static readonly IComparer<WeightedSample> Instance = new WeightedSampleComparer();
        }
    }
}