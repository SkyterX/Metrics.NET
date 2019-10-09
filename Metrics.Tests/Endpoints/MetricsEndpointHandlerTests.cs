using FluentAssertions;
using Metrics.Endpoints;
using Xunit;

namespace Metrics.Tests.Endpoints
{
    public class MetricsEndpointHandlerTests
    {
        [Fact]
        public void MetricsEndpointHandler_CanProcessEndpoint()
        {
            var endpoint = new MetricsEndpoint("test", c => new MetricsEndpointResponse("text", "text/plain"));
            var handler = new TestMetricsEndpointHandler(new[] { endpoint });

            var response = handler.Process("test", null);
            response.Should().NotBeNull();
            response.Content.Should().Be("text");
            response.ContentType.Should().Be("text/plain");
        }

        [Fact]
        public void MetricsEndpointHandler_CannotProcessMissingEndpoint()
        {
            var endpoint = new MetricsEndpoint("test", c => new MetricsEndpointResponse("text", "text/plain"));
            var handler = new MetricsEndpointHandler(new[] { endpoint });

            var response = handler.Process("other", null);
            response.Should().BeNull();
        }
    }
}
