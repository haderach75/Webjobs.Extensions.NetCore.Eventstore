namespace WebJobs.Extensions.EventStore.Impl
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