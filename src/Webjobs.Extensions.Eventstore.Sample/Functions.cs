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
        private readonly Measurement _measurement;
        private const string WebJobDisabledSetting = "WebJobDisabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher, Measurement measurement)
        {
            _eventPublisher = eventPublisher;
            _measurement = measurement;
            _measurement.Start();
        }

        [Disable(WebJobDisabledSetting)]
        public void ProcessQueueMessage([EventTrigger(BatchSize = 2000, TimeOutInMilliSeconds = 50)] IEnumerable<ResolvedEvent> events)
        {
            foreach (var resolvedEvent in events)
            {
                _eventPublisher.Publish(resolvedEvent);
            }
        }

        [Disable(WebJobDisabledSetting)]
        public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
        {
            _measurement.Stop();
        }
    }
}
