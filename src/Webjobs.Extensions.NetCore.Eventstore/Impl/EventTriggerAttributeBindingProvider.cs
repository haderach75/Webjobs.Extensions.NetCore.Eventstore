﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Bindings;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    internal class EventTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly EventStoreConfig _eventStoreConfig;
        private readonly IObserver<SubscriptionContext> _observer;
        private readonly ILoggerFactory _loggerFactory;
        private EventTriggerAttribute _attribute;
        
        public EventTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            EventStoreConfig eventStoreConfig,
            IObserver<SubscriptionContext> observer,
            ILoggerFactory loggerFactory)
        {
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _eventStoreConfig = eventStoreConfig ?? throw new ArgumentNullException(nameof(eventStoreConfig));
            _observer = observer;
            _loggerFactory = loggerFactory;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            ParameterInfo parameter = context.Parameter;
            _attribute = parameter.GetCustomAttribute<EventTriggerAttribute>(inherit: false);
            if (_attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            
            if(_attribute.BatchSize > 2048)
                throw new ArgumentException("Batch size is too big, max size 2048");
            
            _attribute.Stream = Resolve(_attribute.Stream);

            if (string.IsNullOrEmpty(_attribute.TriggerName))
                _attribute.TriggerName = parameter.Member.Name;
            
            if (parameter.ParameterType != typeof(EventTriggerData) &&
                parameter.ParameterType != typeof(IEnumerable<StreamEvent>) &&
                parameter.ParameterType != typeof(IObservable<StreamEvent>))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind EventTriggerAttribute to type '{0}'.", parameter.ParameterType));
            }
            return Task.FromResult<ITriggerBinding>(new EventTriggerBinding(_eventStoreConfig, parameter, _attribute, _observer, _loggerFactory));
        }
        
        private string Resolve(string queueName)
        {
            if (_nameResolver == null)
            {
                return queueName;
            }

            return _nameResolver.ResolveWholeString(queueName);
        }
        
        private class EventTriggerBinding : ITriggerBinding
        {
            private readonly EventStoreConfig _eventStoreConfig;
            private readonly ParameterInfo _parameter;
            private readonly EventTriggerAttribute _attribute;
            private readonly IObserver<SubscriptionContext> _observer;
            private readonly ILoggerFactory _loggerFactory;

            public EventTriggerBinding(EventStoreConfig eventStoreConfig,
                                       ParameterInfo parameter,
                                       EventTriggerAttribute attribute,
                                       IObserver<SubscriptionContext> observer,
                                       ILoggerFactory loggerFactory)
            {
                _eventStoreConfig = eventStoreConfig;
                _parameter = parameter;
                _attribute = attribute;
                _observer = observer;
                _loggerFactory = loggerFactory;
                BindingDataContract = CreateBindingDataContract();
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

            public Type TriggerValueType => typeof(EventTriggerData);

            public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                if (value is string)
                {
                    throw new NotSupportedException("EventTrigger does not support Dashboard invocation.");
                }

                var triggerValue = value as EventTriggerData;
                IValueBinder valueBinder = new EventStoreTriggerValueBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
            {
                var eventStoreSubscription = _eventStoreConfig.EventStoreSubscriptionFactory.Create(_eventStoreConfig, _loggerFactory, _attribute.Stream);
                
                IListener listener = new EventStoreListener(context.Executor,
                                                     eventStoreSubscription,
                                                    _eventStoreConfig.EventFilter,
                                                    _observer,
                                                    _attribute.BatchSize * 2,
                                                    _attribute.TimeOutInMilliSeconds,
                                                    _attribute.TriggerName,
                                                    _loggerFactory.CreateLogger<EventStoreListener>());
                return Task.FromResult(listener);
            }

            public ParameterDescriptor ToParameterDescriptor()
            {
                return new EventTriggerParameterDescriptor
                {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        Prompt = "Event trigger",
                        Description = "Event trigger fired",
                        DefaultValue = "---"
                    }
                };
            }

            private IReadOnlyDictionary<string, object> GetBindingData(EventTriggerData value)
            {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("EventTrigger", value);
                
                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("EventTrigger", typeof(EventTriggerData));
                
                return contract;
            }

            private class EventTriggerParameterDescriptor : TriggerParameterDescriptor
            {
                public override string GetTriggerReason(IDictionary<string, string> arguments)
                {
                    return string.Format("Event trigger fired at {0}", DateTime.Now.ToString("o"));
                }
            }

            private class EventStoreTriggerValueBinder : ValueBinder
            {
                private readonly EventTriggerData _value;

                public EventStoreTriggerValueBinder(ParameterInfo parameter, EventTriggerData value)
                    : base(parameter.ParameterType)
                {
                    _value = value;
                }

                public override Task<object> GetValueAsync()
                {
                    if (Type == typeof(EventTriggerData))
                    {
                        return Task.FromResult<object>(_value);
                    }
                    if (Type == typeof(IObservable<StreamEvent>))
                    {
                        return Task.FromResult<object>(_value.Events.ToObservable());
                    }
                    if (Type == typeof(IEnumerable<StreamEvent>))
                    {
                        return Task.FromResult<object>(_value.Events);
                    }
                    
                    return Task.FromResult<object>(_value);
                }

                public override string ToInvokeString()
                {
                    return "Event trigger";
                }
            }
        }
    }
}
