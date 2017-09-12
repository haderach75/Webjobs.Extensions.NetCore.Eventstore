using System.Configuration;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
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

            var container = new Container();
            container.Options.DefaultScopedLifestyle = new ThreadScopedLifestyle();
            InitíalizeContainer(container);
           
            using (ThreadScopedLifestyle.BeginScope(container))
            {
                config.UseEventStore(new EventStoreConfig
                {
                    ConnectionString = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=20000",
                    Username = "admin",
                    Password = "changeit",
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
