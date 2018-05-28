using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class SubscriptionContext
    {
        public EventStoreCatchUpSubscription Subscription { get; }
        public string EventTriggerName { get; }
        
        public SubscriptionContext(EventStoreCatchUpSubscription subscription, string eventTriggerName)
        {
            Subscription = subscription;
            EventTriggerName = eventTriggerName;
        }
    }
}