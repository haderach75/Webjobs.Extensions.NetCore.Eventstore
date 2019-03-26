using System;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore.Sample
{
    public class EventPublisher : IEventPublisher<StreamEvent>
    {
        public Task PublishAsync(StreamEvent item)
        {
            var e = (StreamEvent<ResolvedEvent>)item;
            var evt = e.Payload;
            if (IsProcessable(evt))
            {
                var json = Encoding.UTF8.GetString(evt.OriginalEvent.Data);
                Console.WriteLine($"Message as JSON: {json}");
            }
            return Task.CompletedTask;
        }
        
        private bool IsProcessable(ResolvedEvent e) {
            var evt = e.Event;
            if (e.OriginalStreamId.StartsWith("$")) return false;
            if (e.Event == null) return false;
            if (!e.Event.IsJson) return false;
            if (evt.EventType == "$streamDeleted") return false;
            if (evt.EventType == "$>") return false; 
            
            return true;
        }
    }
}