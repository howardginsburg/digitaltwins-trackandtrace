using Azure;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.DigitalTwins.Parser;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Demo.Tasks.Client
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Demo tasks = new Demo();
            await tasks.Run();
        }
    }

    class Demo
    {
        private DigitalTwinsClient client = null;
        private string iothubconnection = null;

        private void InitializeClient()
        {
            if (client == null)
            
            {
                Console.WriteLine("Initializing Azure Digital Twins client connection.");
                IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json")
                .Build();
            

                var credential = new DefaultAzureCredential();
                client = new DigitalTwinsClient(new Uri(configuration["ADTInstanceURL"]), credential);

                iothubconnection = configuration["IoTHubSASKey"];
            }
        }
        public async Task Run()
        {
            
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Track and Trace Digital Twins Demo");
                Console.WriteLine($"-----------------------------------------------------------");
                InitializeClient();
                Console.WriteLine($"[1]   Load models");
                Console.WriteLine($"[2]   Load model instance data");
                Console.WriteLine($"[3]   Create IoTHub devices");
                Console.WriteLine($"[4]   Exit");
                
                int selection = -1;
                
                try
                {
                    selection = int.Parse(Console.ReadLine());                
                }
                catch (Exception ex)
                {

                }
               
                if (selection == 1)
                {
                    await HandleLoadModels();
                }
                else if (selection == 2)
                {
                    await HandleDataLoad();
                }
                 else if (selection == 3)
                {
                    await HandleCreateIoTHubDevices();
                }
                else if (selection == 4)
                {
                    return;
                }
            }
        }

        private async Task HandleLoadModels()
        {
            Console.Clear();            
            string consoleAppDir = Path.Combine(Directory.GetCurrentDirectory(), @"Models");
            Console.WriteLine($"Reading from {consoleAppDir}");
            
            try
            {
                List<string> dtdlList = new List<string>();

                IEnumerable<string> dirs = Directory.EnumerateFiles(consoleAppDir,"*.json");
                foreach (string file in dirs)
                {
                    //filename = Path.Combine(consoleAppDir, filenameArray[i]);
                    StreamReader r = new StreamReader(file);
                    string dtdl = r.ReadToEnd();
                    r.Close();
                    dtdlList.Add(dtdl);
                }
                Response<DigitalTwinsModelData[]> res = await client.CreateModelsAsync(dtdlList);
                Console.WriteLine($"Model(s) created successfully!");
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Response {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            
            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey(true);
    
        }

        private async Task HandleDataLoad()
        {
            Console.Clear();            
            string consoleAppDir = Path.Combine(Directory.GetCurrentDirectory(), @"Data");
            Console.WriteLine($"Reading from {consoleAppDir}");

            List<string> relationshipFiles = new List<string>();

            Console.WriteLine("Loading twins...");

            IEnumerable<string> dirs = Directory.EnumerateFiles(consoleAppDir,"*.csv");
            foreach (string file in dirs)
            {
                if (file.Contains("-Relationships"))
                {
                    relationshipFiles.Add(file);
                }
                else
                {
                    await LoadTwin(file);
                }
            }

            Console.WriteLine("Loading relationships...");

            foreach(string file in relationshipFiles)
            {
                await LoadRelationship(file);
            }

            
            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey(true);
    
        }

        private async Task LoadTwin(string file)
        {
            StreamReader r = new StreamReader(file);
            string model = r.ReadLine();
            //Get the headers
            string[] headers = r.ReadLine().Split(',');
            //Get the datatypes
            string[] datatypes = r.ReadLine().Split(',');
            while (!r.EndOfStream)
            {
                string line = r.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    string[] values = line.Split(',');
                    var twinData = new BasicDigitalTwin
                    {
                        Id = values[0],
                        Metadata =
                        {
                            ModelId = model,
                        },
                    };

                    for (int i = 0; i < values.Length; i++)
                    {
                        twinData.Contents.Add(headers[i].Trim(), ConvertStringToType(datatypes[i].Trim(), values[i].Trim()));
                    }

                    try
                    {
                        await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinData.Id, twinData);
                        Console.WriteLine($"Twin '{twinData.Id}' created successfully!");
                    }
                    catch (RequestFailedException e)
                    {
                        Console.WriteLine($"Error {e.Status}: {e.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }
                }
            }
        }

        private async Task LoadRelationship(string file)
        {
            StreamReader r = new StreamReader(file);
            //skip the header.
            r.ReadLine();
            
            while (!r.EndOfStream)
            {
                string line = r.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    string[] values = line.Split(',');

                    var relationship = new BasicRelationship
                    {
                        Id = values[0].Trim(),
                        SourceId = values[2].Trim(),
                        TargetId = values[3].Trim(),
                        Name = values[1].Trim(),
                    };
                    try
                    {
                        await client.CreateOrReplaceRelationshipAsync(relationship.SourceId, relationship.Id, relationship);
                        Console.WriteLine($"Relationship {relationship.Id} of type {relationship.Name} created successfully from {relationship.SourceId} to {relationship.TargetId}!");
                    }
                    catch (RequestFailedException e)
                    {
                        Console.WriteLine($"Error {e.Status}: {e.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }
                }
            }
        }

        private async Task HandleCreateIoTHubDevices()
        {
            Console.Clear();            
            string consoleAppDir = Path.Combine(Directory.GetCurrentDirectory(), @"Data");
            Console.WriteLine($"Reading from {consoleAppDir}");

            string[] files = Directory.GetFiles(consoleAppDir,"*-IoTHub.csv");
            for (int i=0; i<files.Length; i++)
            {
                Console.WriteLine($"Reading {files[i]}");

                string file = files[i];
                StreamReader r = new StreamReader(file);

                //Skip the model
                r.ReadLine();
                //Skip the headers
                r.ReadLine();
                //Skip the datatypes
                r.ReadLine();

                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(iothubconnection);
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        string[] values = line.Split(',');
                        var deviceId = values[0];
                        Console.WriteLine($"Creating device [{deviceId}]");
                        

                        try
                        {
                            Device device = await registryManager.AddDeviceAsync(new Device(deviceId));
                        }
                        catch (DeviceAlreadyExistsException)
                        {
                            Console.WriteLine($"Device {deviceId} exists.  Skipping.");
                        }
                    }
                }
            }

            
            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey(true);
    
        }

        

        private object ConvertStringToType(string schema, string val)
        {
            switch (schema)
            {
                case "boolean":
                    return bool.Parse(val);
                case "double":
                    return double.Parse(val);
                case "float":
                    return float.Parse(val);
                case "integer":
                case "int":
                    return int.Parse(val);
                case "dateTime":
                    return DateTime.Parse(val);
                case "date":
                    return DateTime.Parse(val).Date;
                case "duration":
                    return int.Parse(val);
                case "string":
                default:
                    return val;
            }
        }
    }
}
