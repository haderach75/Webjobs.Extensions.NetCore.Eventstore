using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Webjobs.Extensions.NetCore.Eventstore;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Functions
    {
        private readonly IEventPublisher<ResolvedEvent> _eventPublisher;
        private const string WebJobDisabledSetting = "WebJob_Disabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [Disable(WebJobDisabledSetting)]
        [Singleton(Mode = SingletonMode.Listener)]
        public void ProcessQueueMessage([EventTrigger(BatchSize = 10, TimeOutInMilliSeconds = 20)] IEnumerable<ResolvedEvent> events)
        {
            foreach (var evt in events)
            {
               _eventPublisher.Publish(evt);
            }
        }

        [Disable(WebJobDisabledSetting)]
        public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
        {
            Console.WriteLine("Live started triggered, event stream is now live");
        }
    }
}
