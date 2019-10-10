using System;

using Metrics.ConcurrencyUtilities;
using Metrics.MetricData;
using Metrics.Utils;

namespace Metrics.Core
{
    public class SimpleMeter
    {
        private const long NanosInSecond = 1000L * 1000L * 1000L;
        private const long IntervalSeconds = 5L;
        private const double Interval = IntervalSeconds * NanosInSecond;
        private const double SecondsPerMinute = 60.0;
        private const int OneMinute = 1;
        private const int FiveMinutes = 5;
        private const int FifteenMinutes = 15;

        public void Mark(long count)
        {
            uncounted.Add(count);
        }

        public void Tick()
        {
            var count = uncounted.GetAndReset();
            Tick(count);
        }

        private void Tick(long count)
        {
            total.Add(count);
            var instantRate = count / Interval;
            if (initialized)
            {
                var rate = m1Rate.GetValue();
                m1Rate.SetValue(rate + M1Alpha * (instantRate - rate));

                rate = m5Rate.GetValue();
                m5Rate.SetValue(rate + M5Alpha * (instantRate - rate));

                rate = m15Rate.GetValue();
                m15Rate.SetValue(rate + M15Alpha * (instantRate - rate));
            }
            else
            {
                m1Rate.SetValue(instantRate);
                m5Rate.SetValue(instantRate);
                m15Rate.SetValue(instantRate);
                initialized = true;
            }
        }

        public void Reset()
        {
            uncounted.Reset();
            total.SetValue(0L);
            m1Rate.SetValue(0.0);
            m5Rate.SetValue(0.0);
            m15Rate.SetValue(0.0);
        }

        public MeterValue GetValue(double elapsed)
        {
            var count = total.GetValue() + uncounted.GetValue();
            return new MeterValue(count, GetMeanRate(count, elapsed), OneMinuteRate, FiveMinuteRate, FifteenMinuteRate, TimeUnit.Seconds);
        }

        private static double GetMeanRate(long value, double elapsed)
        {
            if (value == 0)
            {
                return 0.0;
            }

            return value / elapsed * TimeUnit.Seconds.ToNanoseconds(1);
        }

        private double FifteenMinuteRate { get { return m15Rate.GetValue() * NanosInSecond; } }
        private double FiveMinuteRate { get { return m5Rate.GetValue() * NanosInSecond; } }
        private double OneMinuteRate { get { return m1Rate.GetValue() * NanosInSecond; } }
        private static readonly double M1Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / OneMinute);
        private static readonly double M5Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / FiveMinutes);
        private static readonly double M15Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / FifteenMinutes);

        private readonly StripedLongAdder uncounted = new StripedLongAdder();

        private AtomicLong total = new AtomicLong(0L);
        private VolatileDouble m1Rate = new VolatileDouble(0.0);
        private VolatileDouble m5Rate = new VolatileDouble(0.0);
        private VolatileDouble m15Rate = new VolatileDouble(0.0);
        private volatile bool initialized;
    }
}