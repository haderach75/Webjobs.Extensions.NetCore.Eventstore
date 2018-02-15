using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using EventStore.ClientAPI;
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
        private IEventStoreSubscription _eventStoreSubscription;

        /// <summary>
        /// Factory that creates a connection object to event store.
        /// </summary>
        public IEventStoreConnectionFactory EventStoreConnectionFactory { get; set; }
        /// <summary>
        /// Factory used to create user credentials for event store subscription.
        /// </summary>
        public IUserCredentialFactory UserCredentialFactory { get; set; }

        /// <summary>
        /// The position in the stream for the last event processed.
        /// If not position is supplied, the subscription will start from 
        /// the beginning.
        /// </summary>
        public Position? LastPosition { get; set; }
        
        /// <summary>
        /// The username used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// The connection string to the event store cluster.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Queue size for the event store live stream.
        /// </summary>
        public int MaxLiveQueueSize { get; set; }

        /// <summary>
        /// Gets the active event store subscription;
        /// </summary>
        public IEventStoreSubscription EventStoreSubscription
        {
            get => _eventStoreSubscription;
            set => _eventStoreSubscription = value;
        }

        /// <summary>
        /// Gets or set the pre event filtering, which filters event from reaching the trigger.
        /// </summary>
        public IEventFilter EventFilter { get; set; }
        
        public IEventStoreSubscriptionFactory EventStoreSubscriptionFactory { get; set; }
       
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

            if (EventStoreConnectionFactory == null)
                EventStoreConnectionFactory = new EventStoreConnectionFactory();

            if (UserCredentialFactory == null)
                UserCredentialFactory = new UserCredentialFactory();

            if (MaxLiveQueueSize == 0)
                MaxLiveQueueSize = 200;
                
            if(EventStoreSubscriptionFactory == null)
                EventStoreConnectionFactory = new EventStoreConnectionFactory();
            
            if(EventStoreSubscriptionFactory == null)
                EventStoreSubscriptionFactory = new EventStoreSubscriptionFactory();

            var subject = new Subject<IEnumerable<ResolvedEvent>>();
            var triggerBindingProvider = new EventTriggerAttributeBindingProvider(context.Config.NameResolver, this, subject, context.Trace);
            
            var liveProcessingStartedBindingProvider = new LiveProcessingStartedAttributeBindingProvider(subject, context.Trace);

            // Register our extension binding providers
            context.Config.RegisterBindingExtensions(
                triggerBindingProvider);
            context.Config.RegisterBindingExtensions(
                liveProcessingStartedBindingProvider);
        }
    }
 }