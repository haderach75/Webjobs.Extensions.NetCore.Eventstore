using System;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Event
    {
        public Guid EventId { get; }
        public string Name { get; }
        public DateTime Occured { get; }

        public Event(Guid eventId, string name, DateTime occured)
        {
            EventId = eventId;
            Name = name;
            Occured = occured;
        }    
    }
}