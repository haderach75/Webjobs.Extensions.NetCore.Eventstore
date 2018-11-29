using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore.Impl
{
    internal class LiveProcessingStartedAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IObservable<SubscriptionContext> _observable;
        private readonly ILoggerFactory _loggerFactory;
        
        public LiveProcessingStartedAttributeBindingProvider(IObservable<SubscriptionContext> observable, 
                                                             ILoggerFactory loggerFactory)
        {
            _observable = observable;
            _loggerFactory = loggerFactory;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ParameterInfo parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<LiveProcessingStartedAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            if (parameter.ParameterType != typeof(SubscriptionContext))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind LiveProcessingStartedAttribute to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new LiveProcessingStartedTriggerBinding(_observable, parameter, _loggerFactory));
        }

        private class LiveProcessingStartedTriggerBinding : ITriggerBinding
        {
            private readonly IObservable<SubscriptionContext> _observable;
            private readonly ParameterInfo _parameter;
            private readonly ILoggerFactory _loggerFactory;

            public LiveProcessingStartedTriggerBinding(IObservable<SubscriptionContext> observable, 
                                                       ParameterInfo parameter,
                                                       ILoggerFactory loggerFactory)
            {
                _observable = observable;
                _parameter = parameter;
                _loggerFactory = loggerFactory;
                BindingDataContract = CreateBindingDataContract();
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

            public Type TriggerValueType => typeof(SubscriptionContext);

            public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                if (value is string)
                {
                    throw new NotSupportedException("LiveProcessingStartedTrigger does not support Dashboard invocation.");
                }

                var triggerValue = value as SubscriptionContext;
                IValueBinder valueBinder = new LiveProcessingStartedTriggerValueBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
            {
                IListener listener = new LiveProcessingStartedListener(context.Executor,
                    _observable, _loggerFactory.CreateLogger<LiveProcessingStartedListener>());
                return Task.FromResult(listener);
            }
            
            public ParameterDescriptor ToParameterDescriptor()
            {
                return new LiveProcessingStartedTriggerParameterDescriptor
                {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        Prompt = "Live processing trigger",
                        Description = "Live processing trigger fired",
                        DefaultValue = "---"
                    }
                };
            }

            private IReadOnlyDictionary<string, object> GetBindingData(SubscriptionContext value)
            {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("LiveProcessingStartedContext", value);
                
                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("LiveProcessingStarted", typeof(LiveProcessingStartedTriggerValueBinder));

                return contract;
            }
        }
        
        private class LiveProcessingStartedTriggerParameterDescriptor : TriggerParameterDescriptor
        {
            public override string GetTriggerReason(IDictionary<string, string> arguments)
            {
                return string.Format("Live processing started trigger fired at {0}", DateTime.Now.ToString("o"));
            }
        }

        private class LiveProcessingStartedTriggerValueBinder : IOrderedValueBinder
        {
            private readonly SubscriptionContext _value;
                
            public BindStepOrder StepOrder { get; }
                
            public Type Type { get; }

            public LiveProcessingStartedTriggerValueBinder(ParameterInfo parameter, SubscriptionContext value, BindStepOrder bindStepOrder = BindStepOrder.Default)
            {
                StepOrder = bindStepOrder;
                Type = parameter.ParameterType;
                _value = value;
            }

            public Task<object> GetValueAsync()
            {
                if (Type == typeof(SubscriptionContext))
                {
                    return Task.FromResult<object>(_value);
                }
                return Task.FromResult<object>(null);
            }

            public string ToInvokeString()
            {
                return $"Event trigger fired at {DateTime.Now:o}";
            }
                
            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }
    }
}