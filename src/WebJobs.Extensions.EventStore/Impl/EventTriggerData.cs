using System.Collections.Generic;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class EventTriggerData
    {
        public IEnumerable<StreamEvent> Events { get; }
        
        public EventTriggerData(IEnumerable<StreamEvent> events)
        {
            Events = events;
        }
    }
}