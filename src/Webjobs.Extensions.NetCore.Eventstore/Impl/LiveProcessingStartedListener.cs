using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class LiveProcessingStartedListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IObservable<IEnumerable<ResolvedEvent>> _observable;
        private readonly TraceWriter _trace;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observer;
        
        public LiveProcessingStartedListener(ITriggeredFunctionExecutor executor,
                                             IObservable<IEnumerable<ResolvedEvent>> observable,
                                             TraceWriter trace)
        {
            _executor = executor;
            _observable = observable;
            _trace = trace;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _observer = _observable.Subscribe(events => { }, OnCompleted);
            
            return Task.FromResult(true);
        }

        private void OnCompleted()
        {
            var input = new TriggeredFunctionData
            {
                TriggerValue = new LiveProcessingStartedTriggerValue(null)
            };
            _executor.TryExecuteAsync(input, _cancellationToken).Wait();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _trace.Info("Stopping LiveProcessingStartedListener.");
            _observer.Dispose();
            _trace.Info("LiveProcessingStartedListener stopped.");

            return Task.FromResult(true);
        }

        public void Cancel()
        {
            _trace.Info("Cancelling LiveProcessingStartedListener listener.");
            _observer?.Dispose();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _trace.Info("Disposing LiveProcessingStartedListener.");
                Dispose(true);
            }
            _isDisposed = true;
            _trace.Info("LiveProcessingStartedListener disposed.");
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                Cancel();
            }
            GC.SuppressFinalize(this);
        }
    }
}