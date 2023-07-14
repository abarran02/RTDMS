using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace viceroy
{
    public class IotTrigger
    {
        private readonly ILogger log;
        int temperatureThreshold = 76;
        private static readonly HttpClient client = new HttpClient();
        // int minimumEmailInterval = 3600000; // minimum milliseconds between emails, no more than 30/day for IFTTT free tier
        public record ShortTelem(string deviceId, double temperature, double pressure); // IFTTT limits JSON payloads to 3 items
        string iftttUrl = System.Environment.GetEnvironmentVariable("IftttUrl", EnvironmentVariableTarget.Process);

        [FunctionName("IotTrigger")]
        public async Task Run([IoTHubTrigger("messages/events", Connection="ConnectionString")] EventData message, ILogger log)
        {
            // capture the telemetry in an instance of the telemetry model variable
            var jsonPayload = Encoding.UTF8.GetString(message.Body.Array);
            Telem? telemetryDataPoint = JsonSerializer.Deserialize<Telem>(jsonPayload);

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
                log.LogInformation("Successfully sent webhook to IFTTT.");
            }
        }
    }
}
