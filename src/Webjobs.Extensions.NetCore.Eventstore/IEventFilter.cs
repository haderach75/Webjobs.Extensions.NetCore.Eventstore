using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventFilter
    {
        IObservable<ResolvedEvent> Filter(IObservable<ResolvedEvent> eventStreamObservable);
    }
}