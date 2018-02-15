using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

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
            TraceWriter trace) : base(connection, lastCheckpoint, maxLiveQueueMessage, userCredentials, trace)
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
            
            Trace.Info($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
        }
    }
}