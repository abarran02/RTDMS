using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace viceroy
{
    public class IotTrigger
    {
        private readonly ILogger _logger;
        int temperatureThreshold = 76;
        private static readonly HttpClient client = new HttpClient();
        // int minimumEmailInterval = 3600000; // minimum milliseconds between emails, no more than 30/day for IFTTT free tier
        public record ShortTelem(string deviceId, double temperature, double pressure); // IFTTT limits JSON payloads to 3 items
        string iftttUrl = System.Environment.GetEnvironmentVariable("IftttUrl", EnvironmentVariableTarget.Process);

        public IotTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<IotTrigger>();
        }

        [Function("IotTrigger")]
        public async Task Run([EventHubTrigger("messages/events", Connection="ConnectionString")] string[] input)
        {
            // capture the telemetry in an instance of the telemetry model variable
            Telem? telemetryDataPoint = JsonSerializer.Deserialize<Telem>(input[0]);

            if (telemetryDataPoint.temperature > temperatureThreshold)
            {
                await SendEmailNotification(telemetryDataPoint);
            }
        }

        public async Task SendEmailNotification(Telem abnormalDataPoint)
        {
            // send POST request with telemetry as payload
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
            var response = await client.PostAsJsonAsync(
                iftttUrl,
                new ShortTelem(
                    abnormalDataPoint.deviceId,
                    abnormalDataPoint.temperature,
                    abnormalDataPoint.pressure
                )
            );

            // check response code of IFTTT call
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent webhook to IFTTT.");
            }
        }
    }
}
