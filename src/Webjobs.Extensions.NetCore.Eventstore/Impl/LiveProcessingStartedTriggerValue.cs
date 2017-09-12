using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    internal class LiveProcessingStartedTriggerValue
    {
        public EventStoreCatchUpSubscription Subscription { get; }

        public LiveProcessingStartedTriggerValue(EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }
}