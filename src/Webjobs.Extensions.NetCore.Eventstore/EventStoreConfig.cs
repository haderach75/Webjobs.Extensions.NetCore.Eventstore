using System;
using System.Reactive.Subjects;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    /// <summary>
    /// The configuration used to setup the event store subscription and 
    /// binding process.
    /// </summary>
    public class EventStoreConfigProvider : IExtensionConfigProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISubscriptionProvider _subscriptionProvider;
        private readonly EventProcessor _eventProcessor;
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly IEventFilter _eventFilter;
        private readonly IOptions<EventStoreOptions> _options;
        private readonly INameResolver _nameResolver;

        public EventStoreConfigProvider(ILoggerFactory loggerFactory, 
                                        ISubscriptionProvider subscriptionProvider,
                                        EventProcessor eventProcessor,
                                        IEventFilter eventFilter,
                                        INameResolver nameResolver,
                                        IOptions<EventStoreOptions> options)
        {
            _loggerFactory = loggerFactory;
            _subscriptionProvider = subscriptionProvider;
            _eventProcessor = eventProcessor;
            _eventFilter = eventFilter;
            _options = options;
            _nameResolver = nameResolver;
        }
        
        /// <summary>
        /// Method called when the web job starts.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            var subject = new Subject<SubscriptionContext>();
            
            var triggerBindingProvider = new EventTriggerAttributeBindingProvider(_options,
                                                                                  _eventProcessor,
                                                                                  subject,
                                                                                  _nameResolver,
                                                                                  _loggerFactory,
                                                                                  _subscriptionProvider, 
                                                                                  _eventFilter);
            context.AddBindingRule<EventTriggerAttribute>().BindToTrigger(triggerBindingProvider);
            
            var liveProcessingStartedBindingProvider = new LiveProcessingStartedAttributeBindingProvider(subject, _loggerFactory);
            context.AddBindingRule<LiveProcessingStartedAttribute>()
                .BindToTrigger(liveProcessingStartedBindingProvider);
        }
    }
 }