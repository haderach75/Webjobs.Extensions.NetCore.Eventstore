using System;
using System.Reflection;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        public IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, IEventStoreConnectionFactory eventStoreConnectionFactory, ILoggerFactory loggerFactory, string stream = null)
        {
            var eventStoreConnection = eventStoreConnectionFactory.Create(eventStoreOptions.ConnectionString, 
                new EventStoreLogger(loggerFactory), ConnectionName());

            return Create(eventStoreOptions, eventStoreConnection, loggerFactory, stream);
        }

        public IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, IEventStoreConnection eventStoreConnection,
            ILoggerFactory loggerFactory, string stream = null)
        {
            var committedPosition = eventStoreOptions.LastPosition;
            var userCredentials = new UserCredentials(eventStoreOptions.Username, eventStoreOptions.Password);
            
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscription(eventStoreConnection,
                    committedPosition,
                    eventStoreOptions.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<CatchUpSubscription>())
                : new StreamCatchUpSubscription(eventStoreConnection,
                    stream,
                    committedPosition,
                    eventStoreOptions.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<StreamCatchUpSubscription>());
        }

        private static string ConnectionName()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            if (assemblyName.Contains("."))
            {
                return $"{assemblyName.Substring(assemblyName.LastIndexOf('.') + 1)}-{Guid.NewGuid()}";
            }
            return $"{assemblyName}-{Guid.NewGuid()}";;
        }
    }
}