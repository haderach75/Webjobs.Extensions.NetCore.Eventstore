using System.Collections.Generic;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
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