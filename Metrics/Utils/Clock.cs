using System;
using System.Diagnostics;
using System.Globalization;

namespace Metrics.Utils
{
    public abstract class Clock
    {
        public abstract long Nanoseconds { get; }
        public abstract DateTime UTCDateTime { get; }

        public long Seconds { get { return TimeUnit.Nanoseconds.ToSeconds(Nanoseconds); } }

        public static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-ddTHH:mm:ss.ffffK", CultureInfo.InvariantCulture);
        }

        public static readonly Clock SystemDateTime = new SystemClock();
        public static readonly Clock Default = new StopwatchClock();

        private sealed class StopwatchClock : Clock
        {
            public override long Nanoseconds { get { return Stopwatch.GetTimestamp() * factor; } }
            public override DateTime UTCDateTime { get { return DateTime.UtcNow; } }
            private static readonly long factor = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
        }

        private sealed class SystemClock : Clock
        {
            public override long Nanoseconds { get { return DateTime.UtcNow.Ticks * 100L; } }
            public override DateTime UTCDateTime { get { return DateTime.UtcNow; } }
        }
    }

    public static class DateTimeExtensions
    {
        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - unixEpoch).TotalSeconds);
        }

        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().ToUniversalTime();
    }
}