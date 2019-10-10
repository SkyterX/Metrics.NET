using FluentAssertions;

using Metrics.Sampling;

using NUnit.Framework;

namespace Metrics.Tests.Sampling
{
    public class SlidingWindowReservoirTest
    {
        [SetUp]
        public void SetUp()
        {
            reservoir = new SlidingWindowReservoir(3);
        }

        [Test]
        public void SlidingWindowReservoir_CanStoreSmallSample()
        {
            reservoir.Update(1L);
            reservoir.Update(2L);

            reservoir.GetSnapshot().Values.Should().ContainInOrder(1L, 2L);
        }

        [Test]
        public void SlidingWindowReservoir_OnlyStoresLastsValues()
        {
            reservoir.Update(1L);
            reservoir.Update(2L);
            reservoir.Update(3L);
            reservoir.Update(4L);
            reservoir.Update(5L);

            reservoir.GetSnapshot().Values.Should().ContainInOrder(3L, 4L, 5L);
        }

        [Test]
        public void SlidingWindowReservoir_RecordsUserValue()
        {
            reservoir.Update(2L, "B");
            reservoir.Update(1L, "A");

            reservoir.GetSnapshot().MinUserValue.Should().Be("A");
            reservoir.GetSnapshot().MaxUserValue.Should().Be("B");
        }

        private SlidingWindowReservoir reservoir;
    }
}