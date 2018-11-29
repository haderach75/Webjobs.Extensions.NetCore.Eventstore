using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class SubscriptionProvider : ISubscriptionProvider
    {
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;

        public SubscriptionProvider(IEventStoreConnectionFactory eventStoreConnectionFactory)
        {
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
        }
        
        public virtual IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, ILoggerFactory loggerFactory, string stream = null)
        {
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscription(_eventStoreConnectionFactory,
                    eventStoreOptions, loggerFactory.CreateLogger<CatchUpSubscription>())
                : new StreamCatchUpSubscription(_eventStoreConnectionFactory,
                    stream,
                    eventStoreOptions, loggerFactory.CreateLogger<StreamCatchUpSubscription>());
        }
    }
}