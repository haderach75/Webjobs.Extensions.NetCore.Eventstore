using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class SubscriptionProvider : ISubscriptionProvider
    {
        private readonly EventStoreOptions _eventStoreOptions;
        private readonly IMessagePropagator _messagePropagator;
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SubscriptionProvider(IOptions<EventStoreOptions> eventStoreOptions,
                                    IMessagePropagator messagePropagator,
                                    IEventStoreConnectionFactory eventStoreConnectionFactory,
                                    ILoggerFactory loggerFactory)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _messagePropagator = messagePropagator;
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
            _loggerFactory = loggerFactory;
        }
        
        public virtual IEventStoreSubscription Create(string stream = null)
        {
            return string.IsNullOrWhiteSpace(stream)
                ? (IEventStoreSubscription) new CatchUpSubscription(_eventStoreConnectionFactory,
                    _messagePropagator,
                    _eventStoreOptions, 
                    _loggerFactory.CreateLogger<CatchUpSubscription>())
                : new StreamCatchUpSubscription(_eventStoreConnectionFactory,
                    _messagePropagator,
                    stream,
                    _eventStoreOptions, 
                    _loggerFactory.CreateLogger<StreamCatchUpSubscription>());
        }
    }
}