using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore
{
    public interface ISubscriptionProvider
    {
        IEventStoreSubscription Create(EventStoreOptions eventStoreOptions, ILoggerFactory loggerFactory, string stream = null);
    }
}