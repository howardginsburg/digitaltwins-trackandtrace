# digitaltwins-trackandtrace

## Resources
1. Install Docker on your workstation.
2. Create an instance of [Azure Digital Twins](https://ms.portal.azure.com/#create/Microsoft.DigitalTwins).
3. Create an instance of [Azure IoT Hub](https://ms.portal.azure.com/#create/Microsoft.IotHub).
4. Create an instance of [Azure EventHubs](https://ms.portal.azure.com/#create/Microsoft.EventHub) with an eventhub named 'general'.

### Usage
1. Load your digital twin with the track and trace data using the [sample client](client).
2. Start an instance of the [device simulator](https://docs.microsoft.com/en-us/samples/azure-samples/iot-telemetry-simulator/azure-iot-device-telemetry-simulator/) to send data to IoT Hub.

    IotHub Vehicles Simulator

    `docker run -it -e "IoTHubConnectionString=<Your-IoTHub-Connection>" -e "DeviceList=Vehicle1,Vehicle2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\", \"locked\": \"$.Locked\", \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Locked\", \"values\": [\"on\", \"off\"]},{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    IotHub Containers Simulator

    `docker run -it -e "IoTHubConnectionString=<Your-IoTHub-Connection>" -e "DeviceList=Container1,Container2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\",  \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    General Vehicles Simulator

    `docker run -it -e "EventHubConnectionString=<Your-EventHub-Connection>;EntityPath=general" -e "DeviceList=Vehicle1,Vehicle2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\", \"locked\": \"$.Locked\", \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Locked\", \"values\": [\"on\", \"off\"]},{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    General Containers Simulator

    `docker run -it -e "EventHubConnectionString=Endpoint=sb://ehb-trackandtrace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=6YPzk7vacng9AQlccbIx5BnXDzV83c9RqKlJRrROLbM=;EntityPath=general" -e "DeviceList=Container3" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\",  \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`