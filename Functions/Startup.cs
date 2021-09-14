using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.DigitalTwins.Core;
using Azure.Identity;

[assembly: FunctionsStartup(typeof(Demo.TrackandTrace.Startup))]

namespace Demo.TrackandTrace
{
    /**
        CosmosNote - Since Function bindings leverage the Cosmos V2 SDK, we take advantage of dependency injection to be able to get a handle to the
        V3 CosmosHelper object.  Our V2 example does not use this at all.
    */
    public class Startup : FunctionsStartup
    {
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register the DigitalTwins client as a Singleton
            builder.Services.AddSingleton((s) => {
                string digitaltwinsuri = configuration["digitaltwinsuri"];
                if (string.IsNullOrEmpty(digitaltwinsuri))
                {
                    throw new ArgumentNullException("Please specify a valid CosmosDBConnection in the appSettings.json file or your Azure Functions Settings.");
                }

                var credential = new DefaultAzureCredential();
                DigitalTwinsClient client = new DigitalTwinsClient(new Uri(digitaltwinsuri), credential);
                return client;
            });
        }
    }
}