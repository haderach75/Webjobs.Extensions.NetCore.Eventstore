using EventStore.ClientAPI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore
{
    public interface IEventStoreConnectionFactory
    {
        IEventStoreConnection Create(string connectionString, ILogger logger, string connectionName = null);
    }
}