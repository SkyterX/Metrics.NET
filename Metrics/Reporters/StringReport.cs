using System;
using System.Text;
using System.Threading;

using Metrics.MetricData;

namespace Metrics.Reporters
{
    public class StringReport : HumanReadableReport
    {
        public static string RenderMetrics(MetricsData metricsData, Func<HealthStatus> healthStatus)
        {
            var report = new StringReport();
            report.RunReport(metricsData, healthStatus, CancellationToken.None);
            return report.Result;
        }

        protected override void StartReport(string contextName)
        {
            buffer = new StringBuilder();
            base.StartReport(contextName);
        }

        protected override void WriteLine(string line, params string[] args)
        {
            buffer.AppendLine(string.Format(line, args));
        }

        public string Result => buffer.ToString();

        private StringBuilder buffer;
    }
}