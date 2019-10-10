using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics.Sampling
{
    public sealed class UniformSnapshot : Snapshot
    {
        public UniformSnapshot(long count, IEnumerable<long> values, bool valuesAreSorted = false, string minUserValue = null, string maxUserValue = null)
        {
            Count = count;
            this.values = values.ToArray();
            if (!valuesAreSorted)
            {
                Array.Sort(this.values);
            }
            MinUserValue = minUserValue;
            MaxUserValue = maxUserValue;
        }

        public long Count { get; }

        public int Size => values.Length;

        public long Max => values.LastOrDefault();
        public long Min => values.FirstOrDefault();

        public string MaxUserValue { get; }
        public string MinUserValue { get; }

        public double Mean => Size == 0 ? 0.0 : values.Average();

        public double StdDev
        {
            get
            {
                if (Size <= 1)
                {
                    return 0;
                }

                var avg = values.Average();
                var sum = values.Sum(d => Math.Pow(d - avg, 2));

                return Math.Sqrt((sum) / (values.Length - 1));
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

            var pos = quantile * (values.Length + 1);
            var index = (int)pos;

            if (index < 1)
            {
                return values[0];
            }

            if (index >= values.Length)
            {
                return values[values.Length - 1];
            }

            double lower = values[index - 1];
            double upper = values[index];

            return lower + (pos - Math.Floor(pos)) * (upper - lower);
        }

        private readonly long[] values;
    }
}