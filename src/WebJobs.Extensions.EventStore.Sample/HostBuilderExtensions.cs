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
                services.AddSingleton<IEventFilter>(new TestEventFilter());
            });
            return builder;
        }
    }

    public class TestEventFilter : IEventFilter
    {
        public bool Filter(StreamEvent streamEvent)
        {
            if (streamEvent is StreamEvent<ResolvedEvent> resolvedEvent)
            {
                var e = resolvedEvent.Payload;
                var evt = e.Event;
                if (e.OriginalStreamId.StartsWith("$")) return false;
                if (e.OriginalStreamId.StartsWith("checkpoint-")) return false;
                if (e.Event == null) return false;
                if (!e.Event.IsJson) return false;
                if (evt.EventType == "$streamDeleted") return false;
                if (evt.EventType == "$>") return false; // Skip linked events (other projections)
                return true;
            }
            return false; 
        }
    }
}