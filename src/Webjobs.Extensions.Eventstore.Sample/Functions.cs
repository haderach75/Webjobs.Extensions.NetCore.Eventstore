﻿using System;
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
        private const string WebJobDisabledSetting = "WebJobDisabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [Disable(WebJobDisabledSetting)]
        public void ProcessQueueMessage([EventTrigger(BatchSize = 10, TimeOutInMilliSeconds = 20)] IObservable<ResolvedEvent> events)
        {
            events.Subscribe(e => _eventPublisher.Publish(e));
        }

        [Disable(WebJobDisabledSetting)]
        public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
        {
            Console.WriteLine("Live started triggered, event stream is now live");
        }
    }
}
