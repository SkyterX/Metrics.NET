using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics.Reporters
{
    public class ConsoleCSVAppender : CSVAppender
    {
        public ConsoleCSVAppender(string delimiter = CommaDelimiter)
            : base(delimiter)
        {
        }

        public override void AppendLine(DateTime timestamp, string metricType, string metricName, IEnumerable<CSVReport.Value> values)
        {
            Console.WriteLine(GetHeader(values));
            Console.WriteLine(GetValues(timestamp, values));
        }
    }

    public abstract class CSVAppender
    {
        protected CSVAppender(string delimiter)
        {
            if (delimiter == null)
            {
                throw new ArgumentNullException(nameof(delimiter));
            }

            this.delimiter = delimiter;
        }

        public const string CommaDelimiter = ",";

        public abstract void AppendLine(DateTime timestamp, string metricType, string metricName, IEnumerable<CSVReport.Value> values);

        protected virtual string GetHeader(IEnumerable<CSVReport.Value> values)
        {
            return string.Join(delimiter, new[] {"Date", "Ticks"}.Concat(values.Select(v => v.Name)));
        }

        protected virtual string GetValues(DateTime timestamp, IEnumerable<CSVReport.Value> values)
        {
            return string.Join(delimiter, new[] {timestamp.ToString("u"), timestamp.Ticks.ToString("D")}.Concat(values.Select(v => v.FormattedValue)));
        }

        private readonly string delimiter;
    }
}