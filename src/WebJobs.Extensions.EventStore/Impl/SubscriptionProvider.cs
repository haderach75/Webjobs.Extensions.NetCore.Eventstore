using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class SubscriptionProvider : ISubscriptionProvider
    {
        private readonly EventStoreOptions _eventStoreOptions;
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SubscriptionProvider(IOptions<EventStoreOptions> eventStoreOptions, 
                                    IEventStoreConnectionFactory eventStoreConnectionFactory,
                                    ILoggerFactory loggerFactory)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
            _loggerFactory = loggerFactory;
        }
        
        public virtual IEventStoreSubscription Create(string stream = null)
        {
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscription(_eventStoreConnectionFactory,
                    _eventStoreOptions, 
                    _loggerFactory.CreateLogger<CatchUpSubscription>())
                : new StreamCatchUpSubscription(_eventStoreConnectionFactory,
                    stream,
                    _eventStoreOptions, 
                    _loggerFactory.CreateLogger<StreamCatchUpSubscription>());
        }
    }
}