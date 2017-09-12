using System;
using Microsoft.Azure.WebJobs;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public static class EventStoreJobHostConfigurationExtensions
    {
        /// <summary>
        /// Configures and starts an event store subscription and binds
        /// the event trigger.
        /// </summary>
        /// <param name="config">Web job main configuration file</param>
        /// <param name="eventStoreConfig">Event store configuration used in webjob.</param>
        public static void UseEventStore(this JobHostConfiguration config, EventStoreConfig eventStoreConfig = null)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            if (eventStoreConfig == null)
                eventStoreConfig = new EventStoreConfig();

            config.RegisterExtensionConfigProvider(eventStoreConfig);
        }
    }
}
