using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class StreamCatchUpSubscription : SubscriptionBase
    {
        private readonly string _streamName;
        public StreamCatchUpSubscription(IEventStoreConnectionFactory connectionFactory,
            IMessagePropagator messagePropagator,
            string streamName,
            EventStoreOptions options,
            ILogger logger) : base(connectionFactory, messagePropagator, options, logger)
        {
            _streamName = streamName;
        }
        
        protected override void StartCatchUpSubscription(long? startPosition)
        {
//            IsCatchingUp = true;
//            CatchupEventCount = 0;
//            OnCompletedFired = false;
//            IsStarted = true;
//            Connection = EventStoreConnectionFactoryFactory.Create(ConnectionString, Logger);
//            
//            var settings = new CatchUpSubscriptionSettings(MaxLiveQueueMessage, BatchSize, true, false);
//            if (startPosition == AllCheckpoint.AllStart)
//            {
//                var slice = Connection.ReadStreamEventsBackwardAsync(_streamName, Position.End, 1, false, UserCredentials).Result;
//                LastAllPosition = slice.FromPosition;
//            }
//
//            Subscription = Connection.SubscribeToStreamFrom(_streamName,
//                startPosition,
//                settings,
//                EventAppeared,
//                LiveProcessingStarted,
//                SubscriptionDropped,
//                UserCredentials);
//            
//            Logger.LogInformation("Catch-up subscription started from checkpoint {StartPosition} at {Time}.", startPosition, DateTime.Now);
//            CatchupWatch.Restart();
        }
    }
}