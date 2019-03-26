using System;

namespace WebJobs.Extensions.EventStore.Sample
{
    public class Event
    {
        public Guid EventId { get; }
        public string Name { get; }
        public DateTime Occurred { get; }
        
        public long ItemNumber { get; }

        public Event(Guid eventId, string name, DateTime occurred, long itemNumber)
        {
            EventId = eventId;
            Name = name;
            Occurred = occurred;
            ItemNumber = itemNumber;
        }    
    }
}