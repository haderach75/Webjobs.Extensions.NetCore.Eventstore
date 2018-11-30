using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class StreamCatchUpSubscription : SubscriptionBase
    {
        private readonly string _streamName;
        public StreamCatchUpSubscription(IEventStoreConnectionFactory connection,
            string streamName,
            EventStoreOptions options,
            ILogger logger) : base(connection, options, logger)
        {
            _streamName = streamName;
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
            OnCompletedFired = false;
            IsStarted = true;
            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
            Subscription = Connection.SubscribeToStreamFrom(_streamName,
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