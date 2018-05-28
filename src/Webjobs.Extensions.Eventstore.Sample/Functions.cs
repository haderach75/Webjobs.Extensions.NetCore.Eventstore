using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Webjobs.Extensions.NetCore.Eventstore;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Functions
    {
        private readonly IEventPublisher<ResolvedEvent> _eventPublisher;
        private const string WebJobDisabledSetting = "WebJobDisabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [Disable(WebJobDisabledSetting)]
        public Task ProcessEvents([EventTrigger(BatchSize = 2048, TimeOutInMilliSeconds = 50, TriggerName = "Custom trigger name")] IEnumerable<ResolvedEvent> events)
        {
            foreach (var resolvedEvent in events)
            {
                _eventPublisher.Publish(resolvedEvent);
            }
            return Task.CompletedTask;
        }
        
        [Disable(WebJobDisabledSetting)]
        public Task ProcessEvents2([EventTrigger(BatchSize = 1024, TimeOutInMilliSeconds = 50)] IEnumerable<ResolvedEvent> events)
        {
            foreach (var resolvedEvent in events)
            {
                _eventPublisher.Publish(resolvedEvent);
            }
            return Task.CompletedTask;
        }

        [Disable(WebJobDisabledSetting)]
        public void LiveProcessingStarted([LiveProcessingStarted] SubscriptionContext context)
        {
            Console.WriteLine($"Live processing reached for trigger: {context.EventTriggerName}");
        }
    }
}
