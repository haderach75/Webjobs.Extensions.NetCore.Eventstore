using System;
using System.Reflection;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using IESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        public IEventStoreConnection Create(string connectionString, ILogger logger, string connectionName = null)
        {
            var connectionSettings = ConnectionSettings.Create()
                .SetGossipTimeout(TimeSpan.FromMilliseconds(1000))
                .UseCustomLogger(new EventStoreLoggerAdapter(logger))
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
    
    class EventStoreLoggerAdapter : IESLogger
    {
        private readonly ILogger _inner;

        public EventStoreLoggerAdapter(ILogger inner) {
            _inner = inner;
        }

        public void Error(string format, params object[] args) {
            _inner.LogError(format, args);
        }

        public void Error(Exception ex, string format, params object[] args) {
            _inner.LogError(ex, format, args);
        }

        public void Info(string format, params object[] args) {
            _inner.LogInformation(format, args);
        }

        public void Info(Exception ex, string format, params object[] args) {
            _inner.LogInformation(ex, format, args);
        }

        public void Debug(string format, params object[] args) {
            _inner.LogDebug(format, args);
        }

        public void Debug(Exception ex, string format, params object[] args) {
            _inner.LogDebug(ex, format, args);
        }
    }
}