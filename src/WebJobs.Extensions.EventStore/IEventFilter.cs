using System;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore
{
    public interface IEventFilter
    {
        IObservable<StreamEvent> Filter(IObservable<StreamEvent> eventStreamObservable);
    }
}