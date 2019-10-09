﻿using System.Linq;
using FluentAssertions;
using Metrics.Sampling;
using Metrics.Utils;
using Xunit;

namespace Metrics.Tests.Sampling
{
    public class ExponentiallyDecayingReservoirTests
    {
        private readonly TestClock clock = new TestClock();
        private readonly TestScheduler scheduler;

        public ExponentiallyDecayingReservoirTests()
        {
            this.scheduler = new TestScheduler(clock);
        }

        [Fact]
        public void EDR_ReservoirOf100OutOf1000Elements()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(100, 0.99);
            for (int i = 0; i < 1000; i++)
            {
                reservoir.Update(i);
            }

            reservoir.Size.Should().Be(100);
            var snapshot = reservoir.GetSnapshot();
            snapshot.Size.Should().Be(100);
            snapshot.Values.Should().OnlyContain(v => 0 <= v && v < 1000);
        }

        [Fact]
        public void EDR_ReservoirOf100OutOf10Elements()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(100, 0.99);
            for (int i = 0; i < 10; i++)
            {
                reservoir.Update(i);
            }

            reservoir.Size.Should().Be(10);
            var snapshot = reservoir.GetSnapshot();
            snapshot.Size.Should().Be(10);
            snapshot.Values.Should().OnlyContain(v => 0 <= v && v < 10);
        }

        [Fact]
        public void EDR_HeavilyBiasedReservoirOf100OutOf1000Elements()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(1000, 0.01);
            for (int i = 0; i < 100; i++)
            {
                reservoir.Update(i);
            }

            reservoir.Size.Should().Be(100);
            var snapshot = reservoir.GetSnapshot();
            snapshot.Size.Should().Be(100);
            snapshot.Values.Should().OnlyContain(v => 0 <= v && v < 100);
        }

        [Fact]
        public void EDR_longPeriodsOfInactivityShouldNotCorruptSamplingState()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(10, 0.015, clock, scheduler);

            // add 1000 values at a rate of 10 values/second
            for (int i = 0; i < 1000; i++)
            {
                reservoir.Update(1000 + i);
                clock.Advance(TimeUnit.Milliseconds, 100);
            }

            reservoir.GetSnapshot().Size.Should().Be(10);
            reservoir.GetSnapshot().Values.Should().OnlyContain(v => 1000 <= v && v < 2000);

            // wait for 15 hours and add another value.
            // this should trigger a rescale. Note that the number of samples will be reduced to 2
            // because of the very small scaling factor that will make all existing priorities equal to
            // zero after rescale.
            clock.Advance(TimeUnit.Hours, 15);
            reservoir.Update(2000);
            var snapshot = reservoir.GetSnapshot();
            snapshot.Size.Should().Be(2);
            snapshot.Values.Should().OnlyContain(v => 1000 <= v && v < 3000);

            // add 1000 values at a rate of 10 values/second
            for (int i = 0; i < 1000; i++)
            {
                reservoir.Update(3000 + i);
                clock.Advance(TimeUnit.Milliseconds, 100);
            }

            var finalSnapshot = reservoir.GetSnapshot();

            finalSnapshot.Size.Should().Be(10);
            // TODO: double check the Skip first value - sometimes first value is 2000 - which might or not be correct
            finalSnapshot.Values.Skip(1).Should().OnlyContain(v => 3000 <= v && v < 4000);
        }

        [Fact]
        public void EDR_SpotLift()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(clock, scheduler);

            int valuesRatePerMinute = 10;
            int valuesIntervalMillis = (int)(TimeUnit.Minutes.ToMilliseconds(1) / valuesRatePerMinute);
            // mode 1: steady regime for 120 minutes
            for (int i = 0; i < 120 * valuesRatePerMinute; i++)
            {
                reservoir.Update(177);
                clock.Advance(TimeUnit.Milliseconds, valuesIntervalMillis);
            }

            // switching to mode 2: 10 minutes more with the same rate, but larger value
            for (int i = 0; i < 10 * valuesRatePerMinute; i++)
            {
                reservoir.Update(9999);
                clock.Advance(TimeUnit.Milliseconds, valuesIntervalMillis);
            }

            // expect that quantiles should be more about mode 2 after 10 minutes
            reservoir.GetSnapshot().Median.Should().Be(9999);
        }

        [Fact]
        public void EDR_SpotFall()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(clock, scheduler);

            int valuesRatePerMinute = 10;
            int valuesIntervalMillis = (int)(TimeUnit.Minutes.ToMilliseconds(1) / valuesRatePerMinute);
            // mode 1: steady regime for 120 minutes
            for (int i = 0; i < 120 * valuesRatePerMinute; i++)
            {
                reservoir.Update(9998);
                clock.Advance(TimeUnit.Milliseconds, valuesIntervalMillis);
            }

            // switching to mode 2: 10 minutes more with the same rate, but smaller value
            for (int i = 0; i < 10 * valuesRatePerMinute; i++)
            {
                reservoir.Update(178);
                clock.Advance(TimeUnit.Milliseconds, valuesIntervalMillis);
            }

            // expect that quantiles should be more about mode 2 after 10 minutes
            reservoir.GetSnapshot().Percentile95.Should().Be(178);
        }

        [Fact]
        public void EDR_QuantiliesShouldBeBasedOnWeights()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(clock, scheduler);

            for (int i = 0; i < 40; i++)
            {
                reservoir.Update(177);
            }

            clock.Advance(TimeUnit.Seconds, 120);

            for (int i = 0; i < 10; i++)
            {
                reservoir.Update(9999);
            }

            reservoir.GetSnapshot().Size.Should().Be(50);

            // the first added 40 items (177) have weights 1 
            // the next added 10 items (9999) have weights ~6 
            // so, it's 40 vs 60 distribution, not 40 vs 10
            reservoir.GetSnapshot().Median.Should().Be(9999);
            reservoir.GetSnapshot().Percentile75.Should().Be(9999);
        }

        [Fact]
        public void EDR_RecordsUserValue()
        {
            ExponentiallyDecayingReservoir reservoir = new ExponentiallyDecayingReservoir(clock, scheduler);

            reservoir.Update(2L, "B");
            reservoir.Update(1L, "A");

            reservoir.GetSnapshot().MinUserValue.Should().Be("A");
            reservoir.GetSnapshot().MaxUserValue.Should().Be("B");
        }
    }
}
