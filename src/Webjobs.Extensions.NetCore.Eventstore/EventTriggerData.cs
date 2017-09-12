using System.Collections.Generic;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public class EventTriggerData
    {
        public IEnumerable<ResolvedEvent> Events { get; }
        
        public EventTriggerData(IEnumerable<ResolvedEvent> events)
        {
            Events = events;
        }
    }
}