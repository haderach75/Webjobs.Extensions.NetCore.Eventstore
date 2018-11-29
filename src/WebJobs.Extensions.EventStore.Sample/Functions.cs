using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore.Sample
{
    public class FailFastEventProcessor : EventProcessor
    {
        private readonly IJobActivator _jobActivator;
        
        public FailFastEventProcessor(IJobActivator jobActivator)
        {
            _jobActivator = jobActivator;
        }
        
        public override Task CompleteProcessingEventsAsync(IEnumerable<StreamEvent> streamEvents, FunctionResult result, CancellationToken cancellationToken)
        {
            if (result.Exception != null)
            {
                Task.Run(async () =>
                {
                    var jobHost = _jobActivator.CreateInstance<IJobHost>();
                    await jobHost.StopAsync();
                });
            }
            return Task.CompletedTask;
        }
    }
    
    public class Functions
    {
        private readonly IEventPublisher<StreamEvent> _eventPublisher;
        private const string WebJobDisabledSetting = "WebJobDisabled";

        public Functions(IEventPublisher<StreamEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [Disable(WebJobDisabledSetting)]
        public async Task ProcessEvents([EventTrigger(BatchSize = 1024, TimeOutInMilliSeconds = 100, TriggerName = "Custom trigger name")] IEnumerable<StreamEvent> events)
        {
            foreach (var resolvedEvent in events)
            {
                await _eventPublisher.PublishAsync(resolvedEvent);
            }
        }

        public void LiveProcessingStarted([LiveProcessingStarted] SubscriptionContext context)
        {
            Console.WriteLine($"Live processing reached for trigger: {context.EventTriggerName}");
        }
    }
}
