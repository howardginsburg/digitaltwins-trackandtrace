randomValue=$((100 + $RANDOM % 1000))
resourceGroupName="twinssholdemogroup$randomValue"
location="eastus"
twinsName="twinsholdemoadt$randomValue"
storageAccountName="twinsholdemostorage$randomValue"
iotHubName="twinsholdemoiothub$randomValue"
eventHubName="twinsholdemoeventhub$randomValue"
functionAppName="twinsholdemofuncapp$randomValue"

# run az login so that rbac commands can be run.
az login

# allow dynamic extension installs.
az config set extension.use_dynamic_install=yes_without_prompt

# grab the user we're running as and the subscription.
userName=$(az account list --query "[?isDefault].user.name" -o tsv)
subscription=$(az account list --query "[?isDefault].id" -o tsv)

# Create a resource group for the lab.
az group create --location $location --resource-group $resourceGroupName

## Event Hub ###########
# Create an event hub namespace and eventhubs to hold messages coming from IoT Hub and output messages from Digital Twins.
az eventhubs namespace create --resource-group $resourceGroupName --name $eventHubName --location $location --sku Standard
az eventhubs eventhub create --resource-group $resourceGroupName --namespace-name $eventHubName --name messages --message-retention 1 --partition-count 1
az eventhubs eventhub create --resource-group $resourceGroupName --namespace-name $eventHubName --name output --message-retention 1 --partition-count 1
# Create an authorization rule for the output hub.  ADT requires the rule to be at the hub level and not the namespace.
az eventhubs eventhub authorization-rule create --resource-group $resourceGroupName --namespace-name $eventHubName --eventhub-name output --name digitaltwinspolicy --rights {Manage,Send,Listen}
# Get the event hub connection string.
eventHubConnection=$(az eventhubs namespace authorization-rule keys list --resource-group $resourceGroupName --namespace-name $eventHubName --name RootManageSharedAccessKey --query "primaryConnectionString" -o tsv)
#########################

## IoT Hub ##############
az iot hub create --resource-group $resourceGroupName --name $iotHubName --sku S1 -o tsv --query id
# Create a custom end point to the event hub.
az iot hub routing-endpoint create --resource-group $resourceGroupName --hub-name $iotHubName --endpoint-name all-messages --endpoint-type eventhub --endpoint-resource-group $resourceGroupName --endpoint-subscription-id $subscription --connection-string $eventHubConnection";EntityPath=messages"
# Route all messages to the event hub and the build in end point for debugging.
az iot hub route create -g $resourceGroupName --hub-name $iotHubName --endpoint-name all-messages --source DeviceMessages --route-name all_messages --condition true --enabled true
az iot hub route create -g $resourceGroupName --hub-name $iotHubName --endpoint-name events --source DeviceMessages --route-name all_messages_monitor --condition true --enabled true
iotHubOwnerConnection=$(az iot hub connection-string show -n $iotHubName --policy-name iothubowner --key-type primary --query connectionString -o tsv)
#########################

## Azure Digital Twins ######
# Create Azure Digital Twins and wait until it's created.
az dt create -n $twinsName -g $resourceGroupName -l $location
az dt wait -n $twinsName --created
# Add the current user as a twins owner so the console app and twins explorer will work.
az dt role-assignment create -n $twinsName --assignee $userName --role "Azure Digital Twins Data Owner"
# Create an endpoint and route to the output event hub.
az dt endpoint create eventhub --endpoint-name twinupdates --eventhub-resource-group $resourceGroupName --eventhub-namespace $eventHubName --eventhub "output" --eventhub-policy digitaltwinspolicy -n $twinsName
az dt route create -n $twinsName --endpoint-name twinupdates --route-name outputs --filter "type = 'Microsoft.DigitalTwins.Twin.Update'"
# Get the url for the instance for the Azure Function.
twinsUri="https://"$(az dt show --dt-name $twinsName --resource-group $resourceGroupName --query "hostName" -o tsv)
#############################

## Azure Function ###########
# Create a Azure Storage Account 
az storage account create -n $storageAccountName -g $resourceGroupName -l $location --sku "Standard_LRS"
# Create an Azure Function App
az functionapp create --consumption-plan-location $location --name $functionAppName --os-type Windows --resource-group $resourceGroupName --runtime dotnet --storage-account $storageAccountName
# Set the app settings
az functionapp config appsettings set --name $functionAppName --resource-group $resourceGroupName --settings "eventhuburi=$eventHubConnection\";EntityPath=messages\"" "digitaltwinsuri=$twinsUri"
# Enable the managed identity and grant it rights to the Azure Digital Twins instance to be able to update the twins.
az functionapp identity assign -g $resourceGroupName -n $functionAppName
functionAppIdentity=$(az functionapp identity show --name $functionAppName --resource-group $resourceGroupName --query "principalId" -o tsv)
az dt role-assignment create -n $twinsName --assignee $functionAppIdentity --role "Azure Digital Twins Data Owner"
# Download the functions code from github and deploy it.
curl -L https://github.com/howardginsburg/digitaltwins-trackandtrace/blob/main/Functions/functions-publish.zip?raw=true > functions-publish.zip
az functionapp deployment source config-zip --resource-group $resourceGroupName -n $functionAppName --src functions-publish.zip
rm functions-publish.zip

#curl -L https://github.com/howardginsburg/digitaltwins-trackandtrace/archive/refs/heads/main.zip > digitaltwins-trackandtrace.zip
#unzip digitaltwins-trackandtrace.zip

clear

echo "Deployment of Azure resources is successful!"
echo "Resource Group: " $resourceGroupName
echo "ADT URL: " $twinsUri
echo "IoT Hub Owner Policy: " $iotHubOwnerConnection 

