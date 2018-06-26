using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        public IEventStoreConnection Create(string connectionString, ILogger logger, string connectionName = null)
        {
            var connectionSettings = ConnectionSettings.Create()
                .FailOnNoServerResponse()
                .SetGossipTimeout(TimeSpan.FromMilliseconds(1000))
                .UseCustomLogger(logger)
                .KeepReconnecting()
                .KeepRetrying()
                .SetMaxDiscoverAttempts(int.MaxValue);            
            var conn = EventStoreConnection.Create(connectionString, connectionSettings, connectionName);
            
            conn.Connected += (s, e) => logger.Info("Connected to EventStore");
            conn.Disconnected += (s, e) => logger.Info("Disconnected to EventStore");
            conn.Reconnecting += (s, e) => logger.Info("Reconnecting to EventStore...");
            conn.ErrorOccurred += (sender, args) => logger.Error($"Exception ({args.Exception.GetType().Name}): {args.Exception}");
            conn.AuthenticationFailed += (sender, args) => logger.Error($"EventStore authentication failed: {args.Reason}");
            return conn;
        }
    }
}