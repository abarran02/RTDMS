using IotHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.Devices;
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
        private static HttpClient client = new HttpClient();
        static int minimumEmailInterval = 3600000; // minimum milliseconds between emails, no more than 30/day for IFTTT free tier
        static DateTime nextEmailTime = new DateTime();
        public record ShortTelem(string deviceId, double temperature, double pressure); // IFTTT limits JSON payloads to 3 items

        // retrieve environment variables, must be set in local.settings.json or in Azure Host Keys
        static string iftttUrl = System.Environment.GetEnvironmentVariable("IftttUrl", EnvironmentVariableTarget.Process);
        static string connectionString = System.Environment.GetEnvironmentVariable("ConnectionString", EnvironmentVariableTarget.Process);

        [FunctionName("IotTrigger")]
        public static async Task Run([IotHubTrigger("messages/events", Connection = "ConnectionString")] EventData message, ILogger log)
        {
            var jsonPayload = Encoding.UTF8.GetString(message.Body.ToArray());
            // capture the telemetry in an instance of the telemetry model variable
            // nullable to suppress warning CS8632
            #nullable enable
            Telem? telemetryDataPoint = JsonSerializer.Deserialize<Telem>(jsonPayload);
            #nullable disable

            if (telemetryDataPoint.temperature < telemetryDataPoint.temperatureLimit
                && telemetryDataPoint.hvacOn
                && telemetryDataPoint.autoRelay)
            {
                // temperature below threshold, turn HVAC off
                await SendHvacControlC2d("ControlRelay", telemetryDataPoint.deviceId, false);
            }
            else if (telemetryDataPoint.temperature > telemetryDataPoint.temperatureLimit
                && !telemetryDataPoint.hvacOn
                && telemetryDataPoint.autoRelay)
            {
                // temperature exceeds threshold, turn HVAC on
                await SendHvacControlC2d("ControlRelay", telemetryDataPoint.deviceId, true);

                // check if past minimum email time
                if (DateTime.Now > nextEmailTime)
                {
                    await SendEmailNotification(telemetryDataPoint, log);
                }
            }
        }

        public static async Task SendEmailNotification(Telem abnormalDataPoint, ILogger log)
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
                // do not send another email for at least minimumEmailInterval milliseconds (3600000ms == 1hr)
                nextEmailTime = DateTime.Now.AddMilliseconds(minimumEmailInterval);
                var nextEmailString = nextEmailTime.ToString(System.Globalization.CultureInfo.InvariantCulture);
                log.LogInformation($"Successfully sent webhook to IFTTT. Next email at {nextEmailString}.");
            }
            else
            {
                // do not attempt another IFTTT call for one minute
                nextEmailTime = DateTime.Now.AddMinutes(1);
                var nextEmailString = nextEmailTime.ToString(System.Globalization.CultureInfo.InvariantCulture);
                log.LogError($"IFTTT webhook failed. Retrying at {nextEmailString}.");
            }
        }

        public static async Task SendHvacControlC2d(string clientMethodName, string deviceId, bool onoff)
        {
            // initialize method and message
            var methodInvocation = new CloudToDeviceMethod(clientMethodName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30)
            };
            var c2dMessage = new { onoff = onoff };
            string c2dMessageString = System.Text.Json.JsonSerializer.Serialize(c2dMessage);

            // add JSON payload to C2D message
            methodInvocation.SetPayloadJson(c2dMessageString);

            // connect to device and send C2D message
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
        }
    }
}
