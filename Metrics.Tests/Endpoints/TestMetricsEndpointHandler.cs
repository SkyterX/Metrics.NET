using System.Collections.Generic;
using Metrics.Endpoints;

namespace Metrics.Tests.Endpoints
{
    internal class TestMetricsEndpointHandler : AbstractMetricsEndpointHandler<object>
    {
        public TestMetricsEndpointHandler(IEnumerable<MetricsEndpoint> endpoints) : base(endpoints) { }

        protected override MetricsEndpointRequest CreateRequest(object requestInfo)
        {
            return new MetricsEndpointRequest(new Dictionary<string, string[]>());
        }
    }
}
