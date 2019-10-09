using System;
using System.Threading;

using Metrics.MetricData;
using Metrics.Utils;

namespace Metrics.Reporters
{
    public interface MetricsReport : IHideObjectMembers
    {
        void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token);
    }
}