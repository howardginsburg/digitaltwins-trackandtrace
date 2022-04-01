using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure;
using Azure.DigitalTwins.Core;

namespace Demo.TrackandTrace
{
        

    public class DeviceToTwinFunction
    {
        private DigitalTwinsClient _client;
        
        public DeviceToTwinFunction(DigitalTwinsClient client)
        {
            //Get the client object that our Startup.cs creates through dependency injection.
            _client = client;
        }

        [FunctionName("DeviceToTwinFunction")]
        public async Task Run([EventHubTrigger("messages", Connection = "eventhuburi")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(messageBody);

                    var updateTwinData = new JsonPatchDocument();

                    var deviceId = deviceMessage["deviceId"];

                    log.LogInformation($"Processing data for device {deviceId.Value<string>()}: " + messageBody);
                    
                    bool updateTwin = false;
                    try
                    {
                        var temperature = deviceMessage["ambienttemperature"];
                        updateTwinData.AppendAdd("/AmbientTemperature", temperature.Value<double>());
                        updateTwin = true;
                    }
                    catch (Exception e) {}

                    try
                    {
                        var locked = deviceMessage["locked"];
                        bool value = false;
                        if (locked.Value<string>().Equals("on"))
                        {
                            value = true;
                        }
                        updateTwinData.AppendAdd("/Locked", value);
                        updateTwin = true;
                    }
                    catch (Exception e) {}

                    try
                    {
                        var battery = deviceMessage["batterylevel"];
                        updateTwinData.AppendAdd("/BatteryLevel", battery.Value<double>());
                        updateTwin = true;
                    }
                    catch (Exception e) {}

                    if (updateTwin)
                    {
                        await _client.UpdateDigitalTwinAsync(deviceId.Value<string>(), updateTwinData);
                        log.LogInformation("Twin data successfully updated!");
                    }
                    
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
