using HdrHistogram;

using Metrics.ConcurrencyUtilities;

namespace Metrics.Sampling
{
    /// <summary>
    ///     Sampling reservoir based on HdrHistogram.
    ///     Based on the java version from Marshall Pierce https://bitbucket.org/marshallpierce/hdrhistogram-metrics-reservoir/src/83a8ec568a1e?at=master
    /// </summary>
    public sealed class HdrHistogramReservoir : Reservoir
    {
        public HdrHistogramReservoir()
            : this(new Recorder(2))
        {
        }

        internal HdrHistogramReservoir(Recorder recorder)
        {
            this.recorder = recorder;

            intervalHistogram = recorder.GetIntervalHistogram();
            runningTotals = new HdrHistogram.Histogram(intervalHistogram.NumberOfSignificantValueDigits);
        }

        public void Update(long value, string userValue = null)
        {
            recorder.RecordValue(value);
            if (userValue != null)
            {
                TrackMinMaxUserValue(value, userValue);
            }
        }

        public Snapshot GetSnapshot(bool resetReservoir = false)
        {
            var snapshot = new HdrSnapshot(UpdateTotals(), minValue.GetValue(), minUserValue, maxValue.GetValue(), maxUserValue);
            if (resetReservoir)
            {
                Reset();
            }
            return snapshot;
        }

        public void Reset()
        {
            recorder.Reset();
            runningTotals.reset();
            intervalHistogram.reset();
        }

        private HdrHistogram.Histogram UpdateTotals()
        {
            lock (runningTotals)
            {
                intervalHistogram = recorder.GetIntervalHistogram(intervalHistogram);
                runningTotals.add(intervalHistogram);
                return runningTotals.copy() as HdrHistogram.Histogram;
            }
        }

        private void TrackMinMaxUserValue(long value, string userValue)
        {
            if (value > maxValue.NonVolatileGetValue())
            {
                SetMaxValue(value, userValue);
            }

            if (value < minValue.NonVolatileGetValue())
            {
                SetMinValue(value, userValue);
            }
        }

        private void SetMaxValue(long value, string userValue)
        {
            long current;
            while (value > (current = maxValue.GetValue()))
            {
                maxValue.CompareAndSwap(current, value);
            }

            if (value == current)
            {
                lock (maxValueLock)
                {
                    if (value == maxValue.GetValue())
                    {
                        maxUserValue = userValue;
                    }
                }
            }
        }

        private void SetMinValue(long value, string userValue)
        {
            long current;
            while (value < (current = minValue.GetValue()))
            {
                minValue.CompareAndSwap(current, value);
            }

            if (value == current)
            {
                lock (minValueLock)
                {
                    if (value == minValue.GetValue())
                    {
                        minUserValue = userValue;
                    }
                }
            }
        }

        private readonly Recorder recorder;

        private readonly HdrHistogram.Histogram runningTotals;
        private HdrHistogram.Histogram intervalHistogram;

        private AtomicLong maxValue = new AtomicLong(0);
        private string maxUserValue;
        private readonly object maxValueLock = new object();

        private AtomicLong minValue = new AtomicLong(long.MaxValue);
        private string minUserValue;
        private readonly object minValueLock = new object();
    }
}