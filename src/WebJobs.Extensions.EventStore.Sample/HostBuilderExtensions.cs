using System;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore.Sample
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddServices(this IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IEventPublisher<StreamEvent>, EventPublisher>();
                services.AddTransient<Functions>();
                services.AddSingleton<IJobActivator>(provider => new ServiceCollectionJobActivator(provider));
            });
            return builder;
        }
    }
}