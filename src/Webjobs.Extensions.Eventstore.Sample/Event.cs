using System;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Event
    {
        public Guid EventId { get; private set; }
        public string Name { get; private set; }
        public DateTime Occured { get; private set; }

        public Event(Guid eventId, string name, DateTime occured)
        {
            EventId = eventId;
            Name = name;
            Occured = occured;
        }    
    }
}