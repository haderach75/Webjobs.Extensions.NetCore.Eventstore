using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Webjobs.Extensions.NetCore.Eventstore;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Functions
    {
        private readonly IEventPublisher<ResolvedEvent> _eventPublisher;
        private readonly CatchupTimer _catchupTimer;
        private const string WebJobDisabledSetting = "WebJobDisabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher, CatchupTimer catchupTimer)
        {
            _eventPublisher = eventPublisher;
            _catchupTimer = catchupTimer;
            _catchupTimer.Start();
        }

        [Disable(WebJobDisabledSetting)]
        public Task ProcessQueueMessage([EventTrigger(BatchSize = 2048, TimeOutInMilliSeconds = 50)] IEnumerable<ResolvedEvent> events)
        {
            foreach (var resolvedEvent in events)
            {
                _eventPublisher.Publish(resolvedEvent);
            }
            return Task.CompletedTask;
        }

        [Disable(WebJobDisabledSetting)]
        public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
        {
            _catchupTimer.Stop();
        }
    }
}
