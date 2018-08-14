using System;
using System.Reactive.Subjects;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    /// <summary>
    /// The configuration used to setup the event store subscription and 
    /// binding process.
    /// </summary>
    public class EventStoreConfig : IExtensionConfigProvider
    {
        /// <summary>
        /// Factory that creates a connection object to event store.
        /// </summary>
        public IEventStoreConnectionFactory EventStoreConnectionFactory { get; set; } = new EventStoreConnectionFactory();
        
        /// <summary>
        /// The position in the stream for the last event processed.
        /// If not position is supplied, the subscription will start from 
        /// the beginning.
        /// </summary>
        public long? LastPosition { get; set; }

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
        /// Gets or set the pre event filtering, which filters event from reaching the trigger.
        /// </summary>
        public IEventFilter EventFilter { get; set; }
        
        public IEventStoreSubscriptionFactory EventStoreSubscriptionFactory { get; set; } = new EventStoreSubscriptionFactory();
       
        /// <summary>
        /// Method called when jobhost starts.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            var subject = new Subject<SubscriptionContext>();
            var triggerBindingProvider = new EventTriggerAttributeBindingProvider(context.Config.NameResolver, this, subject, context.Config.LoggerFactory);
            
            var liveProcessingStartedBindingProvider = new LiveProcessingStartedAttributeBindingProvider(subject, context.Config.LoggerFactory);

            // Register our extension binding providers
            context.Config.RegisterBindingExtensions(
                triggerBindingProvider);
            context.Config.RegisterBindingExtensions(
                liveProcessingStartedBindingProvider);
        }
    }
 }