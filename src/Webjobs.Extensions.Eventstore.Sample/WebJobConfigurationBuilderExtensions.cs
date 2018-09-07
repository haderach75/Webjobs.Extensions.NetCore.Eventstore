using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public static class WebJobConfigurationBuilderExtensions
    {
        public static IWebJobsBuilder AddServices(this IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IEventPublisher<StreamEvent>, EventPublisher>();
            builder.Services.AddTransient<Functions>();
            builder.Services.AddSingleton<IJobActivator>(provider => new ServiceCollectionJobActivator(provider));
            
            return builder;
        }
    }
}