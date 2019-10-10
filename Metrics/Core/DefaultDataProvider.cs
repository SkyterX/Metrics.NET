using System;
using System.Collections.Generic;
using System.Linq;

using Metrics.MetricData;

namespace Metrics.Core
{
    public class DefaultDataProvider : MetricsDataProvider
    {
        public DefaultDataProvider(string context,
                                   Func<DateTime> timestampProvider,
                                   RegistryDataProvider registryDataProvider,
                                   Func<IEnumerable<MetricsDataProvider>> childProviders)
            : this(context, timestampProvider, Enumerable.Empty<EnvironmentEntry>(), registryDataProvider, childProviders)
        {
        }

        public DefaultDataProvider(string context,
                                   Func<DateTime> timestampProvider,
                                   IEnumerable<EnvironmentEntry> environment,
                                   RegistryDataProvider registryDataProvider,
                                   Func<IEnumerable<MetricsDataProvider>> childProviders)
        {
            this.context = context;
            this.timestampProvider = timestampProvider;
            this.environment = environment;
            this.registryDataProvider = registryDataProvider;
            this.childProviders = childProviders;
        }

        public void SetContextName(string contextName)
        {
            context = contextName;
        }

        public MetricsData CurrentMetricsData
        {
            get
            {
                return new MetricsData(context,
                                       timestampProvider(),
                                       environment,
                                       registryDataProvider.Gauges.ToArray(),
                                       registryDataProvider.Counters.ToArray(),
                                       registryDataProvider.Meters.ToArray(),
                                       registryDataProvider.Histograms.ToArray(),
                                       registryDataProvider.Timers.ToArray(),
                                       childProviders().Select(p => p.CurrentMetricsData));
            }
        }

        private string context;
        private readonly Func<DateTime> timestampProvider;
        private readonly IEnumerable<EnvironmentEntry> environment;
        private readonly RegistryDataProvider registryDataProvider;
        private readonly Func<IEnumerable<MetricsDataProvider>> childProviders;
    }
}