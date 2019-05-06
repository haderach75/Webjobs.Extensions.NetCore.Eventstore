using System;

namespace WebJobs.Extensions.EventStore
{
    public interface IStreamEvent<out TEvent> 
    {  
        TEvent Payload { get;}
    }
    
    public class StreamEvent<TEvent> : StreamEvent, IStreamEvent<TEvent>
    {
        public new TEvent Payload => (TEvent)base.Payload;

        public StreamEvent(object payload) : base(payload, typeof(TEvent))
        {
        }
    }

    public abstract class StreamEvent
    {
        public object Payload { get; }
        public Type Type { get; }

        protected StreamEvent(object payload, Type type)
        {
            Payload = payload;
            Type = type;
        }
    }
}