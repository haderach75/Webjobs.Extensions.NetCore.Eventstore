using System;
using System.Text;
using EventStore.ClientAPI;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class EventPublisher : IEventPublisher<StreamEvent>
    {
        public void Publish(StreamEvent item)
        {
            var e = (StreamEvent<ResolvedEvent>)item;
            var evt = e.Payload;
            var json = Encoding.UTF8.GetString(evt.OriginalEvent.Data);
            
            Console.WriteLine($"Message as JSON: {json}");
        }
    }
}