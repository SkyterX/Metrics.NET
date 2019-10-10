namespace Metrics.MetricData
{
    public struct EnvironmentEntry
    {
        public EnvironmentEntry(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public readonly string Name;
        public readonly string Value;
    }
}