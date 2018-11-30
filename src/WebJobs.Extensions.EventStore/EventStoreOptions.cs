using System;
using System.Threading.Tasks;

namespace WebJobs.Extensions.EventStore
{
    public class EventStoreOptions
    {
        /// <summary>
        /// The username used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Username { get; set; } = "admin";

        /// <summary>
        /// The password used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Password { get; set; } = "changeit";

        /// <summary>
        /// The connection string to the event store cluster.
        /// </summary>
        public string ConnectionString { get; set; } = "ConnectTo=tcp://localhost:1113";

        /// <summary>
        /// Queue size for the event store live stream.
        /// </summary>
        public int MaxLiveQueueSize { get; set; } = 10000;

        /// <summary>
        /// Function to retrieve the last position in the stream for the last event processed.
        /// If not position is supplied, the subscription will start from 
        /// the beginning.
        /// </summary>
        public Func<Task<long>> GetLastPositionAsync { get; set; } = () => Task.FromResult<long>(0);
    }
}