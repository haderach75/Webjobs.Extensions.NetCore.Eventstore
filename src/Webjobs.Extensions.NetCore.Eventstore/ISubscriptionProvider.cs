using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface ISubscriptionProvider
    {
        IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, ILoggerFactory loggerFactory,
            string stream = null);
    }
}