using System;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using WebJobs.Extensions.EventStore.Impl;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Sample
{
    public class EventPublisher : IEventPublisher<StreamEvent>
    {
        private ILogger _logger;
        public EventPublisher(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventPublisher>();
        }
        
        public Task PublishAsync(StreamEvent item)
        {
            var e = (StreamEvent<ResolvedEvent>)item;
            var evt = e.Payload;
            var json = Encoding.UTF8.GetString(evt.OriginalEvent.Data);
            _logger.LogInformation($"Message as JSON: {json}");
            
            return Task.CompletedTask;
        }
    }
}