using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace viceroy
{
    public class IotTrigger
    {
        private readonly ILogger _logger;

        public IotTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<IotTrigger>();
        }

        [Function("IotTrigger")]
        public void Run([EventHubTrigger("messages/events", Connection="ConnectionString")] string[] input)
        {
            // capture the telemetry in an instance of the telemetry model variable
            Telem? telemetryDataPoint = JsonSerializer.Deserialize<Telem>(input[0]);
        }
    }
}
