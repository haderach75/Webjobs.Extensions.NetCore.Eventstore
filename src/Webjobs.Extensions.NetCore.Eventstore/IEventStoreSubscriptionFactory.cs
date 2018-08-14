using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscriptionFactory
    {
        IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, ILoggerFactory loggerFactory, string stream = null);
    }
}