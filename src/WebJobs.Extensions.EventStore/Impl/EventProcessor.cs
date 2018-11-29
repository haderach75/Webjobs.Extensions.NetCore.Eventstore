using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class EventProcessor
    {
        public virtual async Task<bool> BeginProcessingEventsAsync(IEnumerable<StreamEvent> streamEvents, CancellationToken cancellationToken)
        {
            return await Task.FromResult<bool>(true);
        }
        
        public virtual Task CompleteProcessingEventsAsync(IEnumerable<StreamEvent> streamEvents, FunctionResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Succeeded)
            {
                // if the invocation failed, we must propagate the
                // exception back to SB so it can handle message state
                // correctly
                throw result.Exception;
            }

            return Task.CompletedTask;
        }
    }
}