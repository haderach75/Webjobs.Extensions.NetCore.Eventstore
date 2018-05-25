using System.Diagnostics;
using System.IO;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
           
            using (ThreadScopedLifestyle.BeginScope(container))
            {
                InitializeContainer(container);
                config.UseEventStore(new EventStoreConfig
                {
                    ConnectionString = $"{configuration["appSettings:EventStoreConnectionString"]}",
                    Username = $"{configuration["appSettings:EventStoreAdminUser"]}",
                    Password = $"{configuration["appSettings:EventStoreAdminPassword"]}",
                    LastPosition = new Position(0,0),
                    MaxLiveQueueSize = 2000
                });
            }
            
            var jobActivator = new SimpleInjectorJobActivator(container);
            config.JobActivator = jobActivator;
            config.LoggerFactory = container.GetInstance<ILoggerFactory>();
            config.DashboardConnectionString = "";
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        private static void InitializeContainer(Container container)
        {
            container.Register<IEventPublisher<ResolvedEvent>, EventPublisher>(Lifestyle.Singleton);
            container.Register(new LoggerFactory().AddConsole, Lifestyle.Singleton);
            container.RegisterSingleton<Measurement>();
        }
    }

    public class Measurement
    {
        private readonly Stopwatch _sw = new Stopwatch();
        private readonly ILogger<Measurement> _logger;

        public Measurement(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Measurement>();
        }

        public void Start()
        {
            if (!_sw.IsRunning)
            {
                _sw.Start();
                _logger.LogInformation("Stopwatch started.");
            }
        }

        public void Stop()
        {
            _sw.Stop();
            _logger.LogInformation($"Stopwatch stopped, Catchup complete in {_sw.ElapsedMilliseconds}ms");
        }
    }
}
