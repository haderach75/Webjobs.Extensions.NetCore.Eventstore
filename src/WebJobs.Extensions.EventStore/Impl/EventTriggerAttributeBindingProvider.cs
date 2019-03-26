using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebJobs.Extensions.EventStore.Impl
{
    internal class EventTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly ISubscriptionProvider _subscriptionProvider;
        private readonly IEventFilter _eventFilter;
        private readonly IOptions<EventStoreOptions> _eventStoreOptions;
        private readonly EventProcessor _eventProcessor;
        private readonly IObserver<SubscriptionContext> _observer;
        private readonly ILoggerFactory _loggerFactory;
        private EventTriggerAttribute _attribute;

        public EventTriggerAttributeBindingProvider(
            IOptions<EventStoreOptions> eventStoreOptions,
            EventProcessor eventProcessor,
            IObserver<SubscriptionContext> observer,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory, 
            ISubscriptionProvider subscriptionProvider,
            IEventFilter eventFilter)
        {
            _eventStoreOptions = eventStoreOptions ?? throw new ArgumentNullException(nameof(eventStoreOptions));
            _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
            _subscriptionProvider = subscriptionProvider ?? throw new ArgumentNullException(nameof(subscriptionProvider));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _eventFilter = eventFilter ?? throw new ArgumentNullException(nameof(eventFilter));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
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

            if (_attribute.BatchSize > 2048)
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

            return Task.FromResult<ITriggerBinding>(new EventTriggerBinding(_eventStoreOptions, 
                                                    _eventProcessor,
                                                    _subscriptionProvider, 
                                                    _eventFilter, 
                                                    parameter,
                                                    _attribute,
                                                    _observer,
                                                    _loggerFactory));
        }

        private string Resolve(string queueName)
        {
            return _nameResolver == null ? queueName : _nameResolver.ResolveWholeString(queueName);
        }

        private class EventTriggerBinding : ITriggerBinding
        {
            private readonly IOptions<EventStoreOptions> _eventStoreOptions;
            private readonly EventProcessor _eventProcessor;
            private readonly ISubscriptionProvider _subscriptionProvider;
            private readonly IEventFilter _eventFilter;
            private readonly ParameterInfo _parameter;
            private readonly EventTriggerAttribute _attribute;
            private readonly IObserver<SubscriptionContext> _observer;
            private readonly ILoggerFactory _loggerFactory;

            public EventTriggerBinding(IOptions<EventStoreOptions> eventStoreOptions,
                EventProcessor eventProcessor,
                ISubscriptionProvider subscriptionProvider,
                IEventFilter eventFilter,
                ParameterInfo parameter,
                EventTriggerAttribute attribute,
                IObserver<SubscriptionContext> observer,
                ILoggerFactory loggerFactory)
            {
                _eventStoreOptions = eventStoreOptions;
                _eventProcessor = eventProcessor;
                _subscriptionProvider = subscriptionProvider;
                _eventFilter = eventFilter;
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
                var eventStoreSubscription = 
                    _subscriptionProvider.Create(_attribute.Stream);

                IListener listener = new EventStoreListener(context.Executor,
                    _eventProcessor,
                    eventStoreSubscription,
                    _eventFilter,
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
                Dictionary<string, object> bindingData =
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("EventTrigger", value);

                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("EventTrigger", typeof(EventTriggerData));

                return contract;
            }
        }
        
        private class EventTriggerParameterDescriptor : TriggerParameterDescriptor
        {
            public override string GetTriggerReason(IDictionary<string, string> arguments)
            {
                return $"Event trigger fired at {DateTime.Now.ToString("o")}";
            }
        }
        
        private class EventStoreTriggerValueBinder : IOrderedValueBinder
        {
            private readonly EventTriggerData _value;
            private static readonly Type EnumerableStream = typeof(IEnumerable<StreamEvent>);
            private static readonly Type ObservableStream = typeof(IEnumerable<IEnumerable<StreamEvent>>);
                
            public EventStoreTriggerValueBinder(ParameterInfo parameter, EventTriggerData value, BindStepOrder bindStepOrder = BindStepOrder.Default)
            {
                Type = parameter.ParameterType;
                _value = value;
                StepOrder = bindStepOrder;
            }
                
            public BindStepOrder StepOrder { get; }

            public string ToInvokeString()
            {
                return $"Event trigger fired at {DateTime.Now:o}";
            }

            public Type Type { get; }

            public Task<object> GetValueAsync()
            {
                if (Type == EnumerableStream)
                {
                    return Task.FromResult<object>(_value.Events);
                }

                if (Type == ObservableStream)
                {
                    return Task.FromResult<object>(_value.Events.ToObservable());
                }

                return Task.FromResult<object>(_value);
            }
                
            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }
    }
}
