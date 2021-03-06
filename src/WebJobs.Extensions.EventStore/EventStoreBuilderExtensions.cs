﻿using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore
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
           
            builder.Services.TryAddSingleton<IEventStoreConnectionFactory, EventStoreConnectionFactory>();
            builder.Services.TryAddSingleton<ISubscriptionProvider, SubscriptionProvider>();
            builder.Services.TryAddSingleton<INameResolver, NullNameResolver>();
            builder.Services.TryAddSingleton<IEventFilter, NullEventFilter>();
            builder.Services.TryAddSingleton<EventProcessor>();
            builder.Services.TryAddSingleton(provider => 
                new MessagePropagator(
                    provider.GetService<ILogger<MessagePropagator>>(),
                    provider.GetService<IEventFilter>()));
            builder.Services.TryAddSingleton<IMessagePropagator>(provider => provider.GetService<MessagePropagator>());
            
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
           
            builder.Services.Configure(configure);
            builder.AddEventStore();
            
            return builder;
        }
    }
}
