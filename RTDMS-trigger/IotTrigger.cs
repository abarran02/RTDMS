using IotHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;

using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace viceroy
{
    public class IotTrigger
    {
        int temperatureThreshold = 76;
        private static HttpClient client = new HttpClient();
        int minimumEmailInterval = 3600000; // minimum milliseconds between emails, no more than 30/day for IFTTT free tier
        DateTime nextEmailTime = new DateTime();
        public record ShortTelem(string deviceId, double temperature, double pressure); // IFTTT limits JSON payloads to 3 items
        string iftttUrl = System.Environment.GetEnvironmentVariable("IftttUrl", EnvironmentVariableTarget.Process);

        [FunctionName("IotTrigger")]
        public async Task Run([IotHubTrigger("messages/events", Connection = "ConnectionString")] EventData message, ILogger log)
        {
            // capture the telemetry in an instance of the telemetry model variable
            var jsonPayload = Encoding.UTF8.GetString(message.Body.ToArray());
            Telem? telemetryDataPoint = JsonSerializer.Deserialize<Telem>(jsonPayload);

            // check whether current temp is above threshold and past minimum email time
            if (telemetryDataPoint.temperature > temperatureThreshold && DateTime.Now > nextEmailTime)
            {
                await SendEmailNotification(telemetryDataPoint, log);
            }
        }

                public async Task SendEmailNotification(Telem abnormalDataPoint, ILogger log)
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
                // do not send another email for at least an hour
                nextEmailTime = DateTime.Now;
                nextEmailTime.AddMilliseconds(minimumEmailInterval);
            }
            else
            {
                log.LogWarning("IFTTT call failed. Retrying in 1 minute...");
                // do not attempt another IFTTT call for one minute
                nextEmailTime = DateTime.Now;
                nextEmailTime.AddMinutes(1);
            }
        }
    }
}