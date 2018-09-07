using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscriptionFactory
    {
        IEventStoreSubscription Create(EventStoreOptions eventStoreOptions,
            IEventStoreConnectionFactory eventStoreConnectionFactory, ILoggerFactory loggerFactory,
            string stream = null);
    }
}