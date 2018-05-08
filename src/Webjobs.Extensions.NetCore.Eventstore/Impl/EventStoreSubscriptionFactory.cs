using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        private IEventStoreSubscription _eventStoreSubscription;
        
        public IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, ILoggerFactory loggerFactory, string stream = null)
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
                        userCredentials, loggerFactory.CreateLogger<EventStoreCatchUpSubscriptionObservable>())
                    : new EventStoreStreamCatchUpSubscriptionObservable(eventStoreConnection,
                        stream,
                        commitedPosition,
                        eventStoreConfig.MaxLiveQueueSize,
                        userCredentials, loggerFactory.CreateLogger<EventStoreStreamCatchUpSubscriptionObservable>());
            }
            return _eventStoreSubscription;
        }
    }
}