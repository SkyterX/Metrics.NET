using System;
using System.Linq;

using Metrics.ConcurrencyUtilities;

namespace Metrics.Sampling
{
    public sealed class SlidingWindowReservoir : Reservoir
    {
        public SlidingWindowReservoir()
            : this(DefaultSize)
        {
        }

        public SlidingWindowReservoir(int size)
        {
            values = new UserValueWrapper[size];
        }

        private const int DefaultSize = 1028;

        public void Update(long value, string userValue = null)
        {
            var newCount = count.Increment();
            values[(int)((newCount - 1) % values.Length)] = new UserValueWrapper(value, userValue);
        }

        public void Reset()
        {
            Array.Clear(values, 0, values.Length);
            count.SetValue(0L);
        }

        public Snapshot GetSnapshot(bool resetReservoir = false)
        {
            var size = Math.Min((int)count.GetValue(), values.Length);
            if (size == 0)
            {
                return new UniformSnapshot(0, Enumerable.Empty<long>());
            }

            var snapshotValues = new UserValueWrapper[size];
            Array.Copy(values, snapshotValues, size);

            if (resetReservoir)
            {
                Array.Clear(values, 0, snapshotValues.Length);
                count.SetValue(0L);
            }

            Array.Sort(snapshotValues, UserValueWrapper.Comparer);
            var minValue = snapshotValues[0].UserValue;
            var maxValue = snapshotValues[size - 1].UserValue;
            return new UniformSnapshot(count.GetValue(), snapshotValues.Select(v => v.Value), valuesAreSorted : true, minUserValue : minValue, maxUserValue : maxValue);
        }

        private readonly UserValueWrapper[] values;
        private AtomicLong count = new AtomicLong();
    }
}