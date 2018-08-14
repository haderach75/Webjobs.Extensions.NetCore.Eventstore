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
        /// <param name="configAction">Event store configuration.</param>
        public static void UseEventStore(this JobHostConfiguration config, Action<EventStoreConfig> configAction)
        {
            if (configAction == null)
                throw new ArgumentNullException("config");
            
            var eventStoreConfig = new EventStoreConfig();
            configAction(eventStoreConfig);

            config.RegisterExtensionConfigProvider(eventStoreConfig);
        }
    }
}
