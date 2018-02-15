using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreCatchUpSubscriptionObservable : EventStoreStreamSubscriptionObservableBase
    {
        public EventStoreCatchUpSubscriptionObservable(Lazy<IEventStoreConnection> connection,
            long? lastCheckpoint,
            int maxLiveQueueMessage,
            UserCredentials userCredentials,
            TraceWriter trace) : base(connection, lastCheckpoint, maxLiveQueueMessage, userCredentials, trace)
        {
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
            OnCompletedFired = false;
            IsStarted = true;
            var lastPosition = startPosition.HasValue ? new Position(startPosition.Value, startPosition.Value) : AllCheckpoint.AllStart;
            
            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
            Subscription = Connection.Value.SubscribeToAllFrom(
                lastPosition,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                UserCredentials);
            
            Trace.Info($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
        }
    }
}