using System;
using System.Collections.Generic;
using System.Threading;

using Metrics.ConcurrencyUtilities;
using Metrics.Utils;

namespace Metrics.Sampling
{
    public sealed class ExponentiallyDecayingReservoir : Reservoir, IDisposable
    {
        public ExponentiallyDecayingReservoir()
            : this(DefaultSize, DefaultAlpha)
        {
        }

        public ExponentiallyDecayingReservoir(int size, double alpha)
            : this(size, alpha, Clock.Default, new ActionScheduler())
        {
        }

        public ExponentiallyDecayingReservoir(Clock clock, Scheduler scheduler)
            : this(DefaultSize, DefaultAlpha, clock, scheduler)
        {
        }

        public ExponentiallyDecayingReservoir(int size, double alpha, Clock clock, Scheduler scheduler)
        {
            this.size = size;
            this.alpha = alpha;
            this.clock = clock;

            values = new SortedList<double, WeightedSample>(size, ReverseOrderDoubleComparer.Instance);

            rescaleScheduler = scheduler;
            rescaleScheduler.Start(RescaleInterval, () => Rescale());

            startTime = new AtomicLong(clock.Seconds);
        }

        private const int DefaultSize = 1028;
        private const double DefaultAlpha = 0.015;

        public int Size { get { return Math.Min(size, (int)count.GetValue()); } }

        public Snapshot GetSnapshot(bool resetReservoir = false)
        {
            var lockTaken = false;
            try
            {
                @lock.Enter(ref lockTaken);
                var snapshot = new WeightedSnapshot(count.GetValue(), values.Values);
                if (resetReservoir)
                {
                    ResetReservoir();
                }
                return snapshot;
            }
            finally
            {
                if (lockTaken)
                {
                    @lock.Exit();
                }
            }
        }

        public void Update(long value, string userValue = null)
        {
            Update(value, userValue, clock.Seconds);
        }

        public void Reset()
        {
            var lockTaken = false;
            try
            {
                @lock.Enter(ref lockTaken);
                ResetReservoir();
            }
            finally
            {
                if (lockTaken)
                {
                    @lock.Exit();
                }
            }
        }

        private void ResetReservoir()
        {
            values.Clear();
            count.SetValue(0L);
            startTime.SetValue(clock.Seconds);
        }

        private void Update(long value, string userValue, long timestamp)
        {
            var lockTaken = false;
            try
            {
                @lock.Enter(ref lockTaken);

                var itemWeight = Math.Exp(alpha * (timestamp - startTime.GetValue()));
                var sample = new WeightedSample(value, userValue, itemWeight);

                var random = 0.0;
                // Prevent division by 0
                while (random.Equals(.0))
                {
                    random = ThreadLocalRandom.NextDouble();
                }

                var priority = itemWeight / random;

                var newCount = count.GetValue();
                newCount++;
                count.SetValue(newCount);

                if (newCount <= size)
                {
                    values[priority] = sample;
                }
                else
                {
                    var first = values.Keys[values.Count - 1];
                    if (first < priority)
                    {
                        values.Remove(first);
                        values[priority] = sample;
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    @lock.Exit();
                }
            }
        }

        public void Dispose()
        {
            using (rescaleScheduler)
            {
            }
        }

        /// <summary>
        ///     "A common feature of the above techniques—indeed, the key technique that
        ///     allows us to track the decayed weights efficiently—is that they maintain
        ///     counts and other quantities based on g(ti − L), and only scale by g(t − L)
        ///     at query time. But while g(ti −L)/g(t−L) is guaranteed to lie between zero
        ///     and one, the intermediate values of g(ti − L) could become very large. For
        ///     polynomial functions, these values should not grow too large, and should be
        ///     effectively represented in practice by floating point values without loss of
        ///     precision. For exponential functions, these values could grow quite large as
        ///     new values of (ti − L) become large, and potentially exceed the capacity of
        ///     common floating point types. However, since the values stored by the
        ///     algorithms are linear combinations of g values (scaled sums), they can be
        ///     rescaled relative to a new landmark. That is, by the analysis of exponential
        ///     decay in Section III-A, the choice of L does not affect the final result. We
        ///     can therefore multiply each value based on L by a factor of exp(−α(L′ − L)),
        ///     and obtain the correct value as if we had instead computed relative to a new
        ///     landmark L′ (and then use this new L′ at query time). This can be done with
        ///     a linear pass over whatever data structure is being used."
        /// </summary>
        private void Rescale()
        {
            var lockTaken = false;
            try
            {
                @lock.Enter(ref lockTaken);
                var oldStartTime = startTime.GetValue();
                startTime.SetValue(clock.Seconds);

                var scalingFactor = Math.Exp(-alpha * (startTime.GetValue() - oldStartTime));

                var keys = new List<double>(values.Keys);
                foreach (var key in keys)
                {
                    var sample = values[key];
                    values.Remove(key);
                    var newKey = key * Math.Exp(-alpha * (startTime.GetValue() - oldStartTime));
                    var newSample = new WeightedSample(sample.Value, sample.UserValue, sample.Weight * scalingFactor);
                    values[newKey] = newSample;
                }
                // make sure the counter is in sync with the number of stored samples.
                count.SetValue(values.Count);
            }
            finally
            {
                if (lockTaken)
                {
                    @lock.Exit();
                }
            }
        }

        private static readonly TimeSpan RescaleInterval = TimeSpan.FromHours(1);

        private readonly SortedList<double, WeightedSample> values;

        private SpinLock @lock = new SpinLock();

        private readonly double alpha;
        private readonly int size;
        private AtomicLong count = new AtomicLong();
        private AtomicLong startTime;

        private readonly Clock clock;

        private readonly Scheduler rescaleScheduler;

        private class ReverseOrderDoubleComparer : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                return y.CompareTo(x);
            }

            public static readonly IComparer<double> Instance = new ReverseOrderDoubleComparer();
        }
    }
}