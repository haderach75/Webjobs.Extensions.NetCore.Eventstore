using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class CatchUpSubscription : SubscriptionBase
    {
        public CatchUpSubscription(IEventStoreConnectionFactory eventStoreConnectionFactory,
            EventStoreOptions options, ILogger logger) : base(eventStoreConnectionFactory, options, logger)
        {
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
            OnCompletedFired = false;
            IsStarted = true;
            var lastPosition = startPosition.HasValue ? new Position(startPosition.Value, startPosition.Value) : AllCheckpoint.AllStart;
            
            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
            Subscription = Connection.SubscribeToAllFrom(
                lastPosition,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                UserCredentials);
            
            Logger.LogInformation($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
        }
    }
}