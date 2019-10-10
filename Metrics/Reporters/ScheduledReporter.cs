using System;
using System.Threading;

using Metrics.MetricData;
using Metrics.Utils;

namespace Metrics.Reporters
{
    public sealed class ScheduledReporter : IDisposable
    {
        public ScheduledReporter(MetricsReport reporter, MetricsDataProvider metricsDataProvider, Func<HealthStatus> healthStatus, TimeSpan interval)
            : this(reporter, metricsDataProvider, healthStatus, interval, new ActionScheduler())
        {
        }

        public ScheduledReporter(MetricsReport report, MetricsDataProvider metricsDataProvider, Func<HealthStatus> healthStatus, TimeSpan interval, Scheduler scheduler)
        {
            this.report = report;
            this.metricsDataProvider = metricsDataProvider;
            this.healthStatus = healthStatus;
            this.scheduler = scheduler;
            this.scheduler.Start(interval, t => RunReport(t));
        }

        private void RunReport(CancellationToken token)
        {
            report.RunReport(metricsDataProvider.CurrentMetricsData, healthStatus, token);
        }

        public void Dispose()
        {
            using (scheduler)
            {
            }
            using (report as IDisposable)
            {
            }
        }

        private readonly Scheduler scheduler;
        private readonly MetricsReport report;
        private readonly MetricsDataProvider metricsDataProvider;
        private readonly Func<HealthStatus> healthStatus;
    }
}