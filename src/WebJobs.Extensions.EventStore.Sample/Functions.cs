using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore.Sample
{
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
