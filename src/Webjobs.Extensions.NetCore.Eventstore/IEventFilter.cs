using System;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventFilter
    {
        IObservable<StreamEvent> Filter(IObservable<StreamEvent> eventStreamObservable);
    }
}