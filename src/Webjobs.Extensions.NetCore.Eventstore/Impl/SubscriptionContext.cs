using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class SubscriptionContext
    {
        public string EventTriggerName { get; }
        
        public SubscriptionContext(string eventTriggerName)
        {
            EventTriggerName = eventTriggerName;
        }
    }
}