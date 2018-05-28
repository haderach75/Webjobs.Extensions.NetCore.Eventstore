using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreConnectionFactory
    {
        IEventStoreConnection Create(string connectionString, ILogger logger, string connectionName = null);
    }
}