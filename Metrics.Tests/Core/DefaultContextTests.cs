using System;
using System.Linq;

using FluentAssertions;

using Metrics.Core;
using Metrics.MetricData;

using NUnit.Framework;

namespace Metrics.Tests.Core
{
    public class DefaultContextTests
    {
        private MetricsData CurrentData => context.DataProvider.CurrentMetricsData;

        [SetUp]
        public void SetUp()
        {
            context = new DefaultMetricsContext();
        }

        [Test]
        public void MetricsContext_EmptyChildContextIsSameContext()
        {
            var child = context.Context(string.Empty);
            ReferenceEquals(context, child).Should().BeTrue();
            child = context.Context(null);
            ReferenceEquals(context, child).Should().BeTrue();
        }

        [Test]
        public void MetricsContext_ChildWithSameNameAreSameInstance()
        {
            var first = context.Context("test");
            var second = context.Context("test");

            ReferenceEquals(first, second).Should().BeTrue();
        }

        [Test]
        public void MetricsContext_CanCreateSubcontext()
        {
            context.Context("test").Counter("counter", Unit.Requests);

            var counterValue = CurrentData.ChildMetrics.SelectMany(c => c.Counters).Single();

            counterValue.Name.Should().Be("counter");
        }

        [Test]
        public void MetricsContext_MetricsArePresentInMetricsData()
        {
            var counter = context.Counter("test", Unit.Requests);

            counter.Increment();

            var counterValue = CurrentData.Counters.Single();

            counterValue.Name.Should().Be("test");
            counterValue.Unit.Should().Be(Unit.Requests);
            counterValue.Value.Count.Should().Be(1);
        }

        [Test]
        public void MetricsContext_RaisesShutdownEventOnMetricsDisable()
        {
            using (var monitor = ((DefaultMetricsContext)context).Monitor())
            {
                context.Advanced.CompletelyDisableMetrics();
                monitor.Should().Raise("ContextShuttingDown");
            }
        }

        [Test]
        public void MetricsContext_RaisesShutdownEventOnDispose()
        {
            using (var monitor = ((DefaultMetricsContext)context).Monitor())
            {
                context.Dispose();
                monitor.Should().Raise("ContextShuttingDown");
            }
        }

        [Test]
        public void MetricsContext_DataProviderReflectsNewMetrics()
        {
            var provider = context.DataProvider;

            context.Counter("test", Unit.Bytes).Increment();

            provider.CurrentMetricsData.Counters.Should().HaveCount(1);
            provider.CurrentMetricsData.Counters.Single().Name.Should().Be("test");
            provider.CurrentMetricsData.Counters.Single().Value.Count.Should().Be(1L);
        }

        [Test]
        public void MetricsContext_DataProviderReflectsChildContxts()
        {
            var provider = context.DataProvider;

            var counter = context
                .Context("test")
                .Counter("test", Unit.Bytes);

            counter.Increment();

            provider.CurrentMetricsData.ChildMetrics.Should().HaveCount(1);
            provider.CurrentMetricsData.ChildMetrics.Single().Counters.Should().HaveCount(1);
            provider.CurrentMetricsData.ChildMetrics.Single().Counters.Single().Value.Count.Should().Be(1);

            counter.Increment();

            provider.CurrentMetricsData.ChildMetrics.Single().Counters.Single().Value.Count.Should().Be(2);
        }

        [Test]
        public void MetricsContext_DisabledChildContextDoesNotShowInData()
        {
            context.Context("test").Counter("test", Unit.Bytes).Increment();

            CurrentData.ChildMetrics.Single()
                       .Counters.Single().Name.Should().Be("test");

            context.ShutdownContext("test");

            CurrentData.ChildMetrics.Should().BeEmpty();
        }

        [Test]
        public void MetricsContext_DowsNotThrowOnMetricsOfDifferentTypeWithSameName()
        {
            ((Action)(() =>
                {
                    var name = "Test";
                    context.Gauge(name, () => 0.0, Unit.Calls);
                    context.Counter(name, Unit.Calls);
                    context.Meter(name, Unit.Calls);
                    context.Histogram(name, Unit.Calls);
                    context.Timer(name, Unit.Calls);
                })).Should().NotThrow();
        }

        [Test]
        public void MetricsContext_MetricsAddedAreVisibleInTheDataProvider()
        {
            context.DataProvider.CurrentMetricsData.Counters.Should().BeEmpty();
            context.Counter("test", Unit.Bytes);
            context.DataProvider.CurrentMetricsData.Counters.Should().HaveCount(1);
        }

        [Test]
        public void MetricsContext_CanPropagateValueTags()
        {
            context.Counter("test", Unit.None, "tag");
            context.DataProvider.CurrentMetricsData.Counters.Single().Tags.Should().Equal(new[] {"tag"});

            context.Meter("test", Unit.None, tags : "tag");
            context.DataProvider.CurrentMetricsData.Meters.Single().Tags.Should().Equal(new[] {"tag"});

            context.Histogram("test", Unit.None, tags : "tag");
            context.DataProvider.CurrentMetricsData.Histograms.Single().Tags.Should().Equal(new[] {"tag"});

            context.Timer("test", Unit.None, tags : "tag");
            context.DataProvider.CurrentMetricsData.Timers.Single().Tags.Should().Equal(new[] {"tag"});
        }

        private MetricsContext context;
    }
}