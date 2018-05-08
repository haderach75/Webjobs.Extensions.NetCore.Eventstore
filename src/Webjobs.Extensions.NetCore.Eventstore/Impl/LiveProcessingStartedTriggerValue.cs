namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    internal class LiveProcessingStartedTriggerValue
    {
        public EventStore.ClientAPI.EventStoreCatchUpSubscription Subscription { get; }

        public LiveProcessingStartedTriggerValue(EventStore.ClientAPI.EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }
}