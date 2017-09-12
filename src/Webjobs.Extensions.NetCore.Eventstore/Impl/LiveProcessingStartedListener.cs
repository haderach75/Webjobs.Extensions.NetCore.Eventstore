using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class LiveProcessingStartedListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private IEventStoreSubscription _eventStoreSubscription;
        private readonly int _batchSize;
        private readonly int _timeoutInMilliseconds;
        private readonly TraceWriter _trace;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observable;
        
        public LiveProcessingStartedListener(ITriggeredFunctionExecutor executor,
            IEventStoreSubscription eventStoreSubscription,
            int batchSize,
            int timeoutInMilliseconds,
            TraceWriter trace)
        {
            _executor = executor;
            _eventStoreSubscription = eventStoreSubscription;
            _batchSize = batchSize;
            _timeoutInMilliseconds = timeoutInMilliseconds;
            _trace = trace;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _observable = _eventStoreSubscription
                .Buffer(TimeSpan.FromMilliseconds(_timeoutInMilliseconds), _batchSize)
                .Where(buffer => buffer.Any()).Subscribe(x => {}, OnCompleted);
            _eventStoreSubscription.Start(cancellationToken);

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
            _observable.Dispose();
            _eventStoreSubscription.Stop();
            _trace.Info("LiveProcessingStartedListener stopped.");

            return Task.FromResult(true);
        }

        public void Cancel()
        {
            _trace.Info("Cancelling LiveProcessingStartedListener listener.");
            _observable?.Dispose();
            _eventStoreSubscription?.Stop();
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
                _eventStoreSubscription = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}