using System;
using System.Reflection;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        public IEventStoreConnection Create(string connectionString, ILogger logger, string connectionName = null)
        {
            var connectionSettings = ConnectionSettings.Create()
                .SetGossipTimeout(TimeSpan.FromMilliseconds(1000))
                .UseCustomLogger(new EventStoreLogger(logger))
                .KeepReconnecting()
                .KeepRetrying()
                .SetMaxDiscoverAttempts(int.MaxValue);

            connectionName = connectionName ?? ConnectionName();
            var conn = EventStoreConnection.Create(connectionString, connectionSettings, connectionName);
            
            conn.Connected += (s, e) => logger.LogInformation("Connected to EventStore");
            conn.Disconnected += (s, e) => logger.LogInformation("Disconnected to EventStore");
            conn.Reconnecting += (s, e) => logger.LogInformation("Reconnecting to EventStore...");
            conn.ErrorOccurred += (sender, args) => logger.LogInformation($"Exception ({args.Exception.GetType().Name}): {args.Exception}");
            conn.AuthenticationFailed += (sender, args) => logger.LogError($"EventStore authentication failed: {args.Reason}");
            return conn;
        }
        
        private static string ConnectionName()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            if (assemblyName.Contains("."))
            {
                return $"{assemblyName.Substring(assemblyName.LastIndexOf('.') + 1)}-{Guid.NewGuid()}";
            }
            return $"{assemblyName}-{Guid.NewGuid()}";;
        }
    }
}