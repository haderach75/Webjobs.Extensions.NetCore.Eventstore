using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class LiveProcessingStartedListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IObservable<SubscriptionContext> _observable;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private IDisposable _observer;
        
        public LiveProcessingStartedListener(ITriggeredFunctionExecutor executor,
                                             IObservable<SubscriptionContext> observable,
                                             ILogger logger)
        {
            _executor = executor;
            _observable = observable;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _observer = _observable.Subscribe(OnNext);
            
            return Task.FromResult(true);
        }

        private void OnNext(SubscriptionContext context)
        {
            var input = new TriggeredFunctionData
            {
                TriggerValue = context
            };
            _logger.LogDebug("Calling LiveProcessingStartedListener executor");
            _executor.TryExecuteAsync(input, _cancellationTokenSource.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            _logger.LogDebug("LiveProcessingStartedListener executor called");
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
            _cancellationTokenSource.Cancel();
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