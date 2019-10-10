using Metrics.Utils;

namespace Metrics.Core
{
    public sealed class DefaultMetricsContext : BaseMetricsContext
    {
        public DefaultMetricsContext()
            : this(string.Empty)
        {
        }

        public DefaultMetricsContext(string context)
            : base(context, new DefaultMetricsRegistry(), new DefaultMetricsBuilder(), () => Clock.Default.UTCDateTime)
        {
        }

        protected override MetricsContext CreateChildContextInstance(string contextName)
        {
            return new DefaultMetricsContext(contextName);
        }

        public void SetContextName(string contextName)
        {
            ((DefaultDataProvider)DataProvider).SetContextName(contextName);
        }
    }
}