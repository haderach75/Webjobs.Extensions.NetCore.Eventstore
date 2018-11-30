using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore
{
    public interface ISubscriptionProvider
    {
        IEventStoreSubscription Create(string stream = null);
    }
}