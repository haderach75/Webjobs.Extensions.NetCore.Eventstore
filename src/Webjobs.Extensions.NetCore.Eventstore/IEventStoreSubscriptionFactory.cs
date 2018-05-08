using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscriptionFactory
    {
        IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, ILoggerFactory loggerFactory, string stream = null);
    }
}