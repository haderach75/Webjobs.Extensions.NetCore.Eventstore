﻿using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscriptionFactory
    {
        IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, TraceWriter traceWriter,string stream = null);
    }
}