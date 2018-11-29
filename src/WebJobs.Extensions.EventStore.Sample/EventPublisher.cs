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
            var json = Encoding.UTF8.GetString(evt.OriginalEvent.Data);
            
            Console.WriteLine($"Message as JSON: {json}");
            
            throw new InvalidOperationException();

            return Task.CompletedTask;
        }
    }
}