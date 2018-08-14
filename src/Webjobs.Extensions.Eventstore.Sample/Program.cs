using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            var services = new ServiceCollection();
            
            var serviceProvider = InitializeContainer(services);
            config.UseEventStore(options =>
            {
                options.ConnectionString = $"{configuration["appSettings:EventStoreConnectionString"]}";
                options.Username = $"{configuration["appSettings:EventStoreAdminUser"]}";
                options.Password = $"{configuration["appSettings:EventStoreAdminPassword"]}";
                options.LastPosition = 0;
                options.MaxLiveQueueSize = 10000;
            });
                
            var jobActivator = new ServiceCollectionJobActivator(serviceProvider);
            config.JobActivator = jobActivator;
            config.LoggerFactory = serviceProvider.GetService<ILoggerFactory>();
            config.DashboardConnectionString = "";
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        private static IServiceProvider InitializeContainer(IServiceCollection services)
        {
            services.AddSingleton<IEventPublisher<StreamEvent>, EventPublisher>();
            
            var filter = new LogCategoryFilter();
            filter.DefaultLevel = LogLevel.Warning;
            filter.CategoryLevels[LogCategories.Results] = LogLevel.Information;
            filter.CategoryLevels[LogCategories.Aggregator] = LogLevel.Information;
            services.AddSingleton(provider => new LoggerFactory().AddConsole(filter.Filter));

            return services.BuildServiceProvider();
        }
    }
}
