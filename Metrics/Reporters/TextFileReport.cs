using System;
using System.IO;
using System.Text;

namespace Metrics.Reporters
{
    public class TextFileReport : HumanReadableReport
    {
        public TextFileReport(string fileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            this.fileName = fileName;
        }

        protected override void StartReport(string contextName)
        {
            buffer = new StringBuilder();
            base.StartReport(contextName);
        }

        protected override void WriteLine(string line, params string[] args)
        {
            buffer.AppendFormat(line, args);
            buffer.AppendLine();
        }

        protected override void EndReport(string contextName)
        {
            try
            {
                File.WriteAllText(fileName, buffer.ToString());
            }
            catch (Exception x)
            {
                MetricsErrorHandler.Handle(x, "Error writing text file {0}", fileName);
            }

            base.EndReport(contextName);
            buffer = null;
        }

        private readonly string fileName;

        private StringBuilder buffer;
    }
}