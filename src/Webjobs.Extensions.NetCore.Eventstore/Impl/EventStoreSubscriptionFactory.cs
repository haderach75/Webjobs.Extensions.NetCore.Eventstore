using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        private IEventStoreSubscription _eventStoreSubscription;
        
        public IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, TraceWriter traceWriter, string stream = null)
        {
            if (_eventStoreSubscription == null)
            {
                var commitedPosition = eventStoreConfig.LastPosition.HasValue ? eventStoreConfig.LastPosition.Value.CommitPosition : (long?) null;
                var userCredentials = new UserCredentials(eventStoreConfig.Username, eventStoreConfig.Password);
                var eventStoreConnection = eventStoreConfig.EventStoreConnectionFactory.Create(eventStoreConfig.ConnectionString);
                _eventStoreSubscription =  string.IsNullOrWhiteSpace(stream)
                    ? (IEventStoreSubscription) new EventStoreCatchUpSubscriptionObservable(eventStoreConnection,
                        commitedPosition,
                        eventStoreConfig.MaxLiveQueueSize,
                        userCredentials, 
                        traceWriter)
                    : new EventStoreStreamCatchUpSubscriptionObservable(eventStoreConnection,
                        stream,
                        commitedPosition,
                        eventStoreConfig.MaxLiveQueueSize,
                        userCredentials,
                        traceWriter);
            }
            return _eventStoreSubscription;
        }
    }
}