using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class CatchUpSubscription : SubscriptionBase
    {
        public CatchUpSubscription(IEventStoreConnectionFactory eventStoreConnectionFactoryFactory,
            IMessagePropagator messagePropagator,
            EventStoreOptions options, ILogger logger) : base(eventStoreConnectionFactoryFactory, messagePropagator, options, logger)
        {
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
            IsCatchingUp = true;
            CatchupEventCount = 0;
            OnCompletedFired = false;
            
            var lastPosition = startPosition.HasValue ? new Position(startPosition.Value, startPosition.Value) : AllCheckpoint.AllStart;
            
            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
            if (AllCheckpoint.AllStart == null || startPosition == AllCheckpoint.AllStart.Value.CommitPosition)
            {
                var slice = Connection.ReadAllEventsBackwardAsync(Position.End, 1, false, UserCredentials).Result;
                LastAllPosition = slice.FromPosition;
            }

            Subscription = Connection.SubscribeToAllFrom(
                lastPosition,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                UserCredentials);
            
            Logger.LogInformation("Catch-up subscription started from checkpoint {StartPosition} at {Time}.", startPosition, DateTime.Now);
            CatchupWatch.Restart();
        }
    }
}