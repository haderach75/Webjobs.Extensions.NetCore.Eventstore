using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class LiveProcessingStartedListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IObservable<IEnumerable<ResolvedEvent>> _observable;
        private readonly ILogger _logger;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observer;
        
        public LiveProcessingStartedListener(ITriggeredFunctionExecutor executor,
                                             IObservable<IEnumerable<ResolvedEvent>> observable,
                                             ILogger logger)
        {
            _executor = executor;
            _observable = observable;
            _logger = logger;
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
            _logger.LogInformation("Stopping LiveProcessingStartedListener.");
            _observer.Dispose();
            _logger.LogInformation("LiveProcessingStartedListener stopped.");

            return Task.FromResult(true);
        }

        public void Cancel()
        {
            _logger.LogInformation("Cancelling LiveProcessingStartedListener listener.");
            _observer?.Dispose();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _logger.LogInformation("Disposing LiveProcessingStartedListener.");
                Dispose(true);
            }
            _isDisposed = true;
            _logger.LogInformation("LiveProcessingStartedListener disposed.");
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