using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class LiveProcessingStartedContext
    {
        public EventStoreCatchUpSubscription Subscription { get; }
        public LiveProcessingStartedContext(EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }
}