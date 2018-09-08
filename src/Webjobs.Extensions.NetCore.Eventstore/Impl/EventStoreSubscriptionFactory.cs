using System;
using System.Reflection;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        public IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, IEventStoreConnectionFactory eventStoreConnectionFactory, ILoggerFactory loggerFactory, string stream = null)
        {
            var commitedPosition = eventStoreOptions.LastPosition;
            var userCredentials = new UserCredentials(eventStoreOptions.Username, eventStoreOptions.Password);
            var eventStoreConnection = eventStoreConnectionFactory.Create(eventStoreOptions.ConnectionString, 
                new EventStoreLogger(loggerFactory), ConnectionName());
            
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscriptionObservable(eventStoreConnection,
                    commitedPosition,
                    eventStoreOptions.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<CatchUpSubscriptionObservable>())
                : new StreamCatchUpSubscriptionObservable(eventStoreConnection,
                    stream,
                    commitedPosition,
                    eventStoreOptions.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<StreamCatchUpSubscriptionObservable>());
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