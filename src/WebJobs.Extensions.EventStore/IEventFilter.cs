using System;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore
{
    public interface IEventFilter
    {
        IObservable<StreamEvent> Filter(IObservable<StreamEvent> eventStreamObservable);
    }
    
    public static class EventFilterExtensions
    {
        public static IObservable<StreamEvent> ApplyFilter(this IObservable<StreamEvent> eventStreamObservable, IEventFilter eventFilter)
        {
            return eventFilter != null ? eventFilter.Filter(eventStreamObservable) : eventStreamObservable;
        }
    }
}