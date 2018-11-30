using System;

namespace WebJobs.Extensions.EventStore.Sample
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