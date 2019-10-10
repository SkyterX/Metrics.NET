using FluentAssertions;

using NUnit.Framework;

namespace Metrics.Tests.Utils
{
    public class MetricUnitTests
    {
        [TestCase("test", 0, TimeUnit.Seconds, "0.00 s")]
        [TestCase("test", 1, TimeUnit.Nanoseconds, "1.00 ns")]
        [TestCase("test", 1, TimeUnit.Microseconds, "1.00 us")]
        [TestCase("test", 1, TimeUnit.Milliseconds, "1.00 ms")]
        [TestCase("test", 1, TimeUnit.Minutes, "1.00 min")]
        [TestCase("test", 1, TimeUnit.Hours, "1.00 h")]
        [TestCase("test", 1, TimeUnit.Days, "1.00 days")]
        public void Unit_CanFormatDuration(string unit, double value, TimeUnit timeUnit, string output)
        {
            Unit.Custom(unit).FormatDuration(value, timeUnit).Should().Be(output);
        }

        [TestCase("test", 0, "0.00 test")]
        [TestCase("test", 1, "1.00 test")]
        [TestCase("test", 1.2, "1.20 test")]
        [TestCase("test", 1.111, "1.11 test")]
        [TestCase("test", 1.119, "1.12 test")]
        public void Unit_CanFormatDurationWithoutTimeUnit(string unit, double value, string output)
        {
            Unit.Custom(unit).FormatDuration(value, null).Should().Be(output);
        }

        [TestCase("test", 0, TimeUnit.Seconds, "0.00 test/s")]
        [TestCase("test", 1, TimeUnit.Nanoseconds, "1.00 test/ns")]
        [TestCase("test", 1, TimeUnit.Microseconds, "1.00 test/us")]
        [TestCase("test", 1, TimeUnit.Milliseconds, "1.00 test/ms")]
        [TestCase("test", 1, TimeUnit.Minutes, "1.00 test/min")]
        [TestCase("test", 1, TimeUnit.Hours, "1.00 test/h")]
        [TestCase("test", 1, TimeUnit.Days, "1.00 test/days")]
        public void Unit_CanFormatRate(string unit, double value, TimeUnit timeUnit, string output)
        {
            Unit.Custom(unit).FormatRate(value, timeUnit).Should().Be(output);
        }
    }
}