using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Webjobs.Extensions.NetCore.Eventstore;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

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
                config.UseEventStore(options =>
                {
                    options.ConnectionString = $"{configuration["appSettings:EventStoreConnectionString"]}";
                    options.Username = $"{configuration["appSettings:EventStoreAdminUser"]}";
                    options.Password = $"{configuration["appSettings:EventStoreAdminPassword"]}";
                    options.LastPosition = 0;
                    options.MaxLiveQueueSize = 10000;
                });
                
                var jobActivator = new SimpleInjectorJobActivator(container);
                config.JobActivator = jobActivator;
                config.LoggerFactory = container.GetInstance<ILoggerFactory>();
                config.DashboardConnectionString = "";
                var host = new JobHost(config);
                host.RunAndBlock();
            }
        }

        private static void InitializeContainer(Container container)
        {
            container.Register<IEventPublisher<StreamEvent>, EventPublisher>(Lifestyle.Singleton);
            
            var filter = new LogCategoryFilter();
            filter.DefaultLevel = LogLevel.Warning;
            filter.CategoryLevels[LogCategories.Results] = LogLevel.Information;
            filter.CategoryLevels[LogCategories.Aggregator] = LogLevel.Information;
            
            var loggerFactory = new LoggerFactory().AddConsole(filter.Filter);
            
            container.Register(() => loggerFactory, Lifestyle.Singleton);
        }
    }
}
