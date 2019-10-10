using System;
using System.Linq;

using Metrics.ConcurrencyUtilities;

using static System.Math;

namespace Metrics.Sampling
{
    public sealed class UniformReservoir : Reservoir
    {
        public UniformReservoir()
            : this(DefaultSize)
        {
        }

        public UniformReservoir(int size)
        {
            values = new UserValueWrapper[size];
        }

        private const int DefaultSize = 1028;

        public int Size => Min((int)count.GetValue(), values.Length);

        public Snapshot GetSnapshot(bool resetReservoir = false)
        {
            var size = Size;
            if (size == 0)
            {
                return new UniformSnapshot(0, Enumerable.Empty<long>());
            }

            var snapshotValues = new UserValueWrapper[size];
            Array.Copy(values, snapshotValues, size);

            if (resetReservoir)
            {
                count.SetValue(0L);
            }

            Array.Sort(snapshotValues, UserValueWrapper.Comparer);
            var minValue = snapshotValues[0].UserValue;
            var maxValue = snapshotValues[size - 1].UserValue;
            return new UniformSnapshot(count.GetValue(), snapshotValues.Select(v => v.Value), valuesAreSorted : true, minUserValue : minValue, maxUserValue : maxValue);
        }

        public void Update(long value, string userValue = null)
        {
            var c = count.Increment();
            if (c <= values.Length)
            {
                values[(int)c - 1] = new UserValueWrapper(value, userValue);
            }
            else
            {
                var r = ThreadLocalRandom.NextLong(c);
                if (r < values.Length)
                {
                    values[(int)r] = new UserValueWrapper(value, userValue);
                }
            }
        }

        public void Reset()
        {
            count.SetValue(0L);
        }

        private AtomicLong count = new AtomicLong();

        private readonly UserValueWrapper[] values;
    }
}