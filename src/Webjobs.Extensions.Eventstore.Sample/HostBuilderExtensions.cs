using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddServices(this IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<EventProcessor, FailFastEventProcessor>();
                services.AddSingleton<IEventPublisher<StreamEvent>, EventPublisher>();
                services.AddTransient<Functions>();
                services.AddSingleton<IJobActivator>(provider => new ServiceCollectionJobActivator(provider));
            });
            return builder;
        }
    }
}