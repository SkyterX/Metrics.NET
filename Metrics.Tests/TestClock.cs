using System;

using Metrics.Utils;

namespace Metrics.Tests
{
    public sealed class TestClock : Clock
    {
        public override long Nanoseconds => this.nanoseconds;

        public override DateTime UTCDateTime => new DateTime(this.nanoseconds / 100L, DateTimeKind.Utc);

        public void Advance(TimeUnit unit, long value)
        {
            this.nanoseconds += unit.ToNanoseconds(value);
            if (Advanced != null)
            {
                Advanced(this, EventArgs.Empty);
            }
        }

        public event EventHandler Advanced;
        private long nanoseconds = 0;
    }
}