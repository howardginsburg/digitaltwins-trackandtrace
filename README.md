# digitaltwins-trackandtrace

## Deployment of Azure resources

### Automated Deployment

1. Open a Cloud Shell session in the Azure portal or your local workstation.
2. Download the provisioning script using `curl -L https://raw.githubusercontent.com/howardginsburg/digitaltwins-trackandtrace/main/provision.sh > provision.sh`
3. Run `bash provision.sh`

### Manual Deployment

#### Resources

1. Install Docker on your workstation.
2. Create an instance of [Azure Digital Twins](https://ms.portal.azure.com/#create/Microsoft.DigitalTwins).
3. Create an instance of [Azure IoT Hub](https://ms.portal.azure.com/#create/Microsoft.IotHub).
4. Create an instance of [Azure EventHubs](https://ms.portal.azure.com/#create/Microsoft.EventHub) with an eventhubs named 'general' and 'output'.
5. Create an instance of [Azure Functions](https://ms.portal.azure.com/#create/Microsoft.FunctionApp).

#### Azure Configuration

1. Enable System Assigned/Managed Identity on the Function App.
2. Grant the Function Apps Managed Identiy the Digital Twins Owner role on the Azure Digital Twin resource you created.
3. Create a custom route from the IoT Hub to the Event Hub.
4. Deploy the [Functions](Functions) to the Function App.  Make sure you set the 'eventhuburi' and 'digitaltwinsuri' in the Function App Configuration settings.
5. Create a routing endpoint to the 'output' eventhub in your digital twin.
6. Create a route selecting 'Twin Updates' to your endpoint.

## Client App Configuration

1. Rename [sample.local.settings.json](client/sample.local.settings.json) to be 'local.settings.json'.
2. Edit the file and update the 'ADTInstanceURL' and 'IoTHubSASKey' configuration settings.

## Running the Simulation

1. Load your digital twin with the track and trace data using the [sample client](client).
2. Start instance(s) of the [device simulator](https://docs.microsoft.com/en-us/samples/azure-samples/iot-telemetry-simulator/azure-iot-device-telemetry-simulator/) to send data to IoT Hub.

    IotHub Vehicles Simulator

    `docker run -it -e "IoTHubConnectionString=<Your-IoTHub-Connection>" -e "DeviceList=Vehicle1,Vehicle2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\", \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    IotHub Containers Simulator

    `docker run -it -e "IoTHubConnectionString=<Your-IoTHub-Connection>" -e "DeviceList=Container1,Container2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\",  \"locked\": \"$.Locked\",\"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Locked\", \"values\": [\"on\", \"off\"]},{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    General Vehicles Simulator

    `docker run -it -e "EventHubConnectionString=<Your-EventHub-Connection>;EntityPath=general" -e "DeviceList=Vehicle1,Vehicle2" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\", \"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

    General Containers Simulator

    `docker run -it -e "EventHubConnectionString=<Your-EventHub-Connection;EntityPath=general" -e "DeviceList=Container3" -e Template="{\"deviceId\": \"$.DeviceId\", \"time\": \"$.Time\",  \"locked\": \"$.Locked\",\"batterylevel\": \"$.Battery\", \"ambienttemperature\": \"$.AmbientTemperature\"}" -e Variables="[{\"name\": \"Locked\", \"values\": [\"on\", \"off\"]},{\"name\": \"Battery\", \"random\": true, \"max\": 100, \"min\": 0},{\"name\": \"AmbientTemperature\", \"random\": true, \"max\": 90, \"min\": 50}]" -e "MessageCount=0" -e "Interval=5000" mcr.microsoft.com/oss/azure-samples/azureiot-telemetrysimulator:latest`

3. Monitor the digital twins in the Digital Twins Explorer app to see changes.
   * All Twins: `SELECT * FROM DIGITALTWINS`
   * All Containers: `SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:container;1')`
   * All Vehicles:`SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:vehicle;1')`
   * All Trackable Twins (Containers and Vehicles): `SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:trackablebase;1')`
   * All Containers that are unlocked:`SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:container;1') and Locked = false`
   * All Containers and Vehicles with an Ambient Temperature > 70: `SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:trackablebase;1') and AmbientTemperature > 70`
   * All Containers and Vehicles with a Battery Level < 25: `SELECT * FROM DIGITALTWINS T WHERE IS_OF_MODEL(T, 'dtmi:com:microsoft:iot:trackandtrace:trackablebase;1') and BatteryLevel < 25`
   * All Containers on Vehicle1: `SELECT Container FROM DIGITALTWINS Vehicle JOIN Container RELATED Vehicle.rel_has_containers WHERE Vehicle.$dtId = 'Vehicle2' AND IS_OF_MODEL(Container, 'dtmi:com:microsoft:iot:trackandtrace:container;1')`
   * All Containers for Fleet1 and Fleet2: `SELECT Container FROM DIGITALTWINS Fleet JOIN Vehicle RELATED Fleet.rel_has_vehicles JOIN Container RELATED Vehicle.rel_has_containers WHERE IS_OF_MODEL(Vehicle, 'dtmi:com:microsoft:iot:trackandtrace:vehicle;1') AND IS_OF_MODEL(Container, 'dtmi:com:microsoft:iot:trackandtrace:container;1') AND Fleet.$dtId IN ['Fleet1', 'Fleet2']`
   * All Containers for Fleet1 and Fleet2 where the AmbientTemperature for the container is > 50: `SELECT Container FROM DIGITALTWINS Fleet JOIN Vehicle RELATED Fleet.rel_has_vehicles JOIN Container RELATED Vehicle.rel_has_containers WHERE IS_OF_MODEL(Vehicle, 'dtmi:com:microsoft:iot:trackandtrace:vehicle;1') AND IS_OF_MODEL(Container, 'dtmi:com:microsoft:iot:trackandtrace:container;1') AND Fleet.$dtId IN ['Fleet1', 'Fleet2'] AND Container.AmbientTemperature > 50`

4. Monitor the 'output' eventhub for twin updates that can be processed by downstream systems.