using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreSubscriptionFactory : IEventStoreSubscriptionFactory
    {
        private IEventStoreSubscription _eventStoreSubscription;
        
        public IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, ILoggerFactory loggerFactory, string stream = null)
        {
            if (_eventStoreSubscription != null) return _eventStoreSubscription;
            
            var commitedPosition = eventStoreConfig.LastPosition?.CommitPosition;
            var userCredentials = new UserCredentials(eventStoreConfig.Username, eventStoreConfig.Password);
            var eventStoreConnection = eventStoreConfig.EventStoreConnectionFactory.Create(eventStoreConfig.ConnectionString, 
                new EventStoreLogger(loggerFactory));
            _eventStoreSubscription =  string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscriptionObservable(eventStoreConnection,
                    commitedPosition,
                    eventStoreConfig.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<CatchUpSubscriptionObservable>())
                : new StreamCatchUpSubscriptionObservable(eventStoreConnection,
                    stream,
                    commitedPosition,
                    eventStoreConfig.MaxLiveQueueSize,
                    userCredentials, loggerFactory.CreateLogger<StreamCatchUpSubscriptionObservable>());
            return _eventStoreSubscription;
        }
    }
}