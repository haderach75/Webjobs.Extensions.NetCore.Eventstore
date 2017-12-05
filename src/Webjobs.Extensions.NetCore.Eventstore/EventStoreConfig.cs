using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
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
        public Func<Position?> LastPosition { get; set; }

        /// <summary>
        /// Factory used to create an event store listener.
        /// </summary>
        public IListenerFactory EventStoreListenerFactory { get; set; }
        
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
        public IEventStoreSubscription EventStoreSubscription => _eventStoreSubscription;

        /// <summary>
        /// Gets or set the pre event filtering, which filters event from reaching the trigger.
        /// </summary>
        public IEventFilter EventFilter { get; set; }
       
        private IEventStoreSubscription _eventStoreSubscription;
        private int _batchSize = 100;
        private int _timeOutInMilliSeconds = 50;

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

            var lastPosition = LastPosition?.Invoke();
           
            _eventStoreSubscription = new EventStoreCatchUpSubscriptionObservable(EventStoreConnectionFactory.Create(ConnectionString),
                lastPosition,
                MaxLiveQueueSize,
                UserCredentialFactory.CreateAdminCredentials(Username, Password), 
                context.Trace);

            var triggerBindingProvider = new EventTriggerAttributeBindingProvider<EventTriggerAttribute>(
                BuildListener, context.Config, context.Trace);

            var liveProcessingStartedBindingProvider = new LiveProcessingStartedAttributeBindingProvider(
                BuildListener, context.Trace);

            // Register our extension binding providers
            context.Config.RegisterBindingExtensions(
                triggerBindingProvider);
            context.Config.RegisterBindingExtensions(
                liveProcessingStartedBindingProvider);
        }

        private Task<IListener> BuildListener(EventTriggerAttribute attribute,
                                              ITriggeredFunctionExecutor executor, 
                                              TraceWriter trace)
        {
            IListener listener;
            _batchSize = attribute.BatchSize;
            
            if (EventStoreListenerFactory == null)
            {
                listener = new EventStoreListener(executor, _eventStoreSubscription, EventFilter, trace)
                {
                    BatchSize = _batchSize,
                    TimeOutInMilliSeconds = _timeOutInMilliSeconds = attribute.TimeOutInMilliSeconds
                };
            }
            else
            {
                listener = EventStoreListenerFactory.Create();
            }
            return Task.FromResult(listener);
        }

        private Task<IListener> BuildListener(ITriggeredFunctionExecutor executor, TraceWriter trace)
        {
            IListener listener = new LiveProcessingStartedListener(executor, 
                _eventStoreSubscription,
                _batchSize,
                _timeOutInMilliSeconds,
                trace);
            return Task.FromResult<IListener>(listener);
        }


    }
}