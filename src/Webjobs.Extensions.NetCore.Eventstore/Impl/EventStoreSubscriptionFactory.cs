using System;
using System.Reflection;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        public IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, ILoggerFactory loggerFactory, string stream = null)
        {
            var commitedPosition = eventStoreConfig.LastPosition?.CommitPosition;
            var userCredentials = new UserCredentials(eventStoreConfig.Username, eventStoreConfig.Password);
            var eventStoreConnection = eventStoreConfig.EventStoreConnectionFactory.Create(eventStoreConfig.ConnectionString, 
                new EventStoreLogger(loggerFactory), ConnectionName());
            
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscriptionObservable(eventStoreConnection,
                    commitedPosition,
                    eventStoreConfig.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<CatchUpSubscriptionObservable>())
                : new StreamCatchUpSubscriptionObservable(eventStoreConnection,
                    stream,
                    commitedPosition,
                    eventStoreConfig.MaxLiveQueueSize,
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