using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Bindings;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    internal class EventTriggerAttributeBindingProvider<TAttribute> : ITriggerBindingProvider where TAttribute : Attribute
    {
        private readonly Func<TAttribute, ITriggeredFunctionExecutor, TraceWriter, Task<IListener>> _listenerBuilder;
        private readonly TraceWriter _trace;
        private readonly JobHostConfiguration _config;

        public EventTriggerAttributeBindingProvider(
            Func<TAttribute, ITriggeredFunctionExecutor, TraceWriter, Task<IListener>> listenerBuilder,
            JobHostConfiguration config,
            TraceWriter trace)
        {
            _listenerBuilder = listenerBuilder;
            _config = config;
            _trace = trace;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<TAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            
            if (parameter.ParameterType != typeof(EventTriggerData) &&
                parameter.ParameterType != typeof(IEnumerable<ResolvedEvent>))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind EventTriggerAttribute to type '{0}'.", parameter.ParameterType));
            }
            
            return Task.FromResult<ITriggerBinding>(new EventTriggerBinding(_config, parameter, attribute, _trace, this));
        }
        
        private class EventTriggerBinding : ITriggerBinding
        {
            private readonly JobHostConfiguration _config;
            private readonly ParameterInfo _parameter;
            private readonly TAttribute _attribute;
            private readonly TraceWriter _trace;
            private readonly EventTriggerAttributeBindingProvider<TAttribute> _parent;

            public EventTriggerBinding(JobHostConfiguration config,
                ParameterInfo parameter,
                TAttribute attribute,
                TraceWriter trace,
                EventTriggerAttributeBindingProvider<TAttribute> parent)
            {
                _config = config;
                _parameter = parameter;
                _attribute = attribute;
                _trace = trace;
                BindingDataContract = CreateBindingDataContract();
                _parent = parent;
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

            public Type TriggerValueType => typeof(EventStoreTriggerValue);

            public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                if (value is string)
                {
                    throw new NotSupportedException("EventTrigger does not support Dashboard invocation.");
                }

                var triggerValue = value as EventStoreTriggerValue;
                IValueBinder valueBinder = new EventStoreTriggerValueBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
            {
                Task<IListener> listener = _parent._listenerBuilder(_attribute, context.Executor, _trace);
                return listener;
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

            private IReadOnlyDictionary<string, object> GetBindingData(EventStoreTriggerValue value)
            {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("EventTrigger", value);
                bindingData.Add("EventTriggerData", value);
                bindingData.Add("IEnumerable<ResolvedEvent>", value.Events);

                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("EventTrigger", typeof(EventStoreTriggerValueBinder));
                
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
                private readonly object _value;

                public EventStoreTriggerValueBinder(ParameterInfo parameter, EventStoreTriggerValue value)
                    : base(parameter.ParameterType)
                {
                    _value = value;
                }

                public override Task<object> GetValueAsync()
                {
                    if (Type == typeof(EventTriggerData))
                    {
                        var triggerData = (EventStoreTriggerValue) _value;
                        var data = new EventTriggerData(triggerData.Events);
                        return Task.FromResult<object>(data);
                    }
                    if (Type == typeof(IEnumerable<ResolvedEvent>))
                    {
                        var triggerData = (EventStoreTriggerValue)_value;
                        return Task.FromResult<object>(triggerData.Events);
                    }
                    return Task.FromResult(_value);
                }

                public override string ToInvokeString()
                {
                    return "Event trigger";
                }
            }
        }
    }
}
