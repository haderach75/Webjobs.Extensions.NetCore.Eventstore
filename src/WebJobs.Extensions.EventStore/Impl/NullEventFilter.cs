using System;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class NullEventFilter : IEventFilter
    {
        public IObservable<StreamEvent> Filter(IObservable<StreamEvent> eventStreamObservable)
        {
            return eventStreamObservable;
        }
    }
}