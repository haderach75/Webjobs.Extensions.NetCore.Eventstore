using System.Configuration;
using System.IO;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Webjobs.Extensions.NetCore.Eventstore;

namespace Webjobs.Extensions.Eventstore.Sample
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new ThreadScopedLifestyle();
            InitíalizeContainer(container);
           
            using (ThreadScopedLifestyle.BeginScope(container))
            {
                config.UseEventStore(new EventStoreConfig
                {
                    ConnectionString = $"{configuration["appSettings:EventStoreConnectionString"]}",
                    Username = $"{configuration["appSettings:EventStoreAdminUser"]}",
                    Password = $"{configuration["appSettings:EventStoreAdminPassword"]}",
                    LastPosition = new Position(0,0),
                    MaxLiveQueueSize = 500
                });
            }

            var jobActivator = new SimpleInjectorJobActivator(container);
            config.JobActivator = jobActivator;
            //config.DashboardConnectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsDashboard"].ConnectionString;
            //config.StorageConnectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        private static void InitíalizeContainer(Container container)
        {
            container.Register<IEventPublisher<ResolvedEvent>, EventPublisher>();
        }
    }
}
