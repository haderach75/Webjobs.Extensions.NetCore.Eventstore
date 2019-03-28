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
            if (IsProcessable(evt))
            {
                var json = Encoding.UTF8.GetString(evt.OriginalEvent.Data);
                _logger.LogDebug($"Message as JSON: {json}");
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