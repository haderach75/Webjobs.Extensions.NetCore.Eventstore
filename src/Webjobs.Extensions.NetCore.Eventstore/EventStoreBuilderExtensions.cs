using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public static class EventStoreBuilderExtensions
    {
        /// <summary>
        /// Configures and starts an event store subscription and binds
        /// the event trigger.
        /// </summary>
        public static IWebJobsBuilder AddEventStore(this IWebJobsBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddExtension<EventStoreConfigProvider>()
                .BindOptions<EventStoreOptions>();

            builder.Services.AddSingleton<IEventFilter, NullEventFilter>();
            builder.Services.AddSingleton<IEventStoreConnectionFactory, EventStoreConnectionFactory>();
            builder.Services.AddSingleton<IEventStoreSubscriptionFactory, EventStoreSubscriptionFactory>();
            builder.Services.AddSingleton<INameResolver, NullNameResolver>();

            return builder;
        }

        /// <summary>
        /// Configures and starts an event store subscription and binds
        /// the event trigger.
        /// </summary>
        /// <param name="builder">Web job builder.</param>
        /// <param name="configure">Action delegate to configure options.</param>
        public static IWebJobsBuilder AddEventStore(this IWebJobsBuilder builder, Action<EventStoreOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            
            builder.AddEventStore();
            builder.Services.Configure(configure);
          
            return builder;
        }
    }
}
