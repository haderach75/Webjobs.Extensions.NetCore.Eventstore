using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreConnectionFactory
    {
        Lazy<IEventStoreConnection> Create(string connectionString);
    }
}