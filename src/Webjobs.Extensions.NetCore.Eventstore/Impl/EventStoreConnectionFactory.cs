using System;
using System.Diagnostics;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        public Lazy<IEventStoreConnection> Create(string connectionString)
        {
            return new Lazy<IEventStoreConnection>(() => OpenConnection(connectionString), true);
        }

        public static IEventStoreConnection OpenConnection(string connectionString)
        {
            var conn = CreateConnection(connectionString);
            conn.Connected += (s, e) => Trace.TraceInformation("Connected to EventStore");
            conn.Reconnecting += (s, e) => Trace.TraceInformation("Reconnecting to EventStore...");
            conn.Disconnected += (s, e) => Trace.TraceInformation("Disconnected to EventStore");
            conn.ErrorOccurred += (sender, args) => Trace.TraceError($"Exception ({args.Exception.GetType().Name}): {args.Exception}");
            conn.AuthenticationFailed += (sender, args) => Trace.TraceError($"EventStore authentication failed: {args.Reason}");
            conn.ConnectAsync().Wait();
            return conn;
        }

        public static IEventStoreConnection CreateConnection(string connectionString)
        {
            // Use this connectionstring for cluster:
            // GossipSeeds=...;GossipTimeout=1000;MaxReconnections=1000;ReconnectionDelay=2000;HeartbeatTimeout=5000
            return EventStoreConnection.Create(connectionString);
        }
    }
}