using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Metrics.Core;
using Metrics.MetricData;
using Metrics.Sampling;

using NUnit.Framework;

namespace Metrics.Tests.Core
{
    public class DefaultContextCustomMetricsTests
    {
        [SetUp]
        public void SetUp()
        {
            context = new DefaultMetricsContext();
        }

        [Test]
        public void MetricsContext_CanRegisterCustomCounter()
        {
            var counter = context.Advanced.Counter("custom", Unit.Calls, () => new CustomCounter());
            counter.Should().BeOfType<CustomCounter>();
            counter.Increment();
            context.DataProvider.CurrentMetricsData.Counters.Single().Value.Count.Should().Be(10L);
        }

        [Test]
        public void MetricsContext_CanRegisterTimerWithCustomReservoir()
        {
            var reservoir = new CustomReservoir();
            var timer = context.Advanced.Timer("custom", Unit.Calls, () => (Reservoir)reservoir);

            timer.Record(10L, TimeUnit.Nanoseconds);

            reservoir.Size.Should().Be(1);
            reservoir.Values.Single().Should().Be(10L);
        }

        [Test]
        public void MetricsContext_CanRegisterTimerWithCustomHistogram()
        {
            var histogram = new CustomHistogram();

            var timer = context.Advanced.Timer("custom", Unit.Calls, () => (HistogramImplementation)histogram);

            timer.Record(10L, TimeUnit.Nanoseconds);

            histogram.Reservoir.Size.Should().Be(1);
            histogram.Reservoir.Values.Single().Should().Be(10L);
        }

        private MetricsContext context;

        public class CustomCounter : CounterImplementation
        {
            public void Increment()
            {
            }

            public void Increment(long value)
            {
            }

            public void Decrement()
            {
            }

            public void Decrement(long value)
            {
            }

            public void Increment(string item)
            {
            }

            public void Increment(string item, long value)
            {
            }

            public void Decrement(string item)
            {
            }

            public void Decrement(string item, long value)
            {
            }

            public void Reset()
            {
            }

            public CounterValue GetValue(bool resetMetric = false)
            {
                return Value;
            }

            public CounterValue Value => new CounterValue(10L, new CounterValue.SetItem[0]);
        }

        public class CustomReservoir : Reservoir
        {
            public long Count => values.Count;
            public int Size => values.Count;
            public IEnumerable<long> Values => values;

            public void Update(long value, string userValue)
            {
                values.Add(value);
            }

            public Snapshot GetSnapshot(bool resetReservoir = false)
            {
                return new UniformSnapshot(values.Count, values);
            }

            public void Reset()
            {
                values.Clear();
            }

            private readonly List<long> values = new List<long>();
        }

        public class CustomHistogram : HistogramImplementation
        {
            public CustomReservoir Reservoir { get; } = new CustomReservoir();

            public void Update(long value, string userValue)
            {
                Reservoir.Update(value, userValue);
            }

            public void Reset()
            {
                Reservoir.Reset();
            }

            public HistogramValue GetValue(bool resetMetric = false)
            {
                return Value;
            }

            public HistogramValue Value => new HistogramValue(Reservoir.Values.Last(), null, Reservoir.GetSnapshot());
        }
    }
}