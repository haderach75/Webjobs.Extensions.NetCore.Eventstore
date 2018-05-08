using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreStreamCatchUpSubscriptionObservable : EventStoreStreamSubscriptionObservableBase
    {
        private readonly string _stream;
        public EventStoreStreamCatchUpSubscriptionObservable(Lazy<IEventStoreConnection> connection, 
            string stream,
            long? lastCheckpoint,
            int maxLiveQueueMessage,
            UserCredentials userCredentials,
            ILogger logger) : base(connection, lastCheckpoint, maxLiveQueueMessage, userCredentials, logger)
        {
            _stream = stream;
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
            OnCompletedFired = false;
            IsStarted = true;
            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
            Subscription = Connection.Value.SubscribeToStreamFrom(_stream,
                startPosition,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                UserCredentials);
            
            Logger.LogInformation($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
        }
    }
}