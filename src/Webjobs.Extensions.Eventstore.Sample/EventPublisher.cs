using System;
using System.Diagnostics;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class EventPublisher : IEventPublisher<ResolvedEvent>
    {
        private int _updateCounter = 0;
        public void Publish(ResolvedEvent item)
        {
            var json = Encoding.UTF8.GetString(item.OriginalEvent.Data);
            JsonConvert.DeserializeObject<Event>(json);
            Console.WriteLine($"Message as JSON: {json}");
            
            if (_updateCounter++ % 50000 == 0) Console.WriteLine($"{DateTime.Now:T}: Events processed #{_updateCounter}");
        }
    }
}