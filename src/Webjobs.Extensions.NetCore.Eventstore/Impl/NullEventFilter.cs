using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class NullEventFilter : IEventFilter
    {
        public IObservable<StreamEvent> Filter(IObservable<StreamEvent> eventStreamObservable)
        {
            return eventStreamObservable;
        }
    }
}