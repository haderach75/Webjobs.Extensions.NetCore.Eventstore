using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class EventStoreListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly EventProcessor _eventProcessor;
        private MessagePropagator _messagePropagator;
        private IEventStoreSubscription _eventStoreSubscription;
        private readonly IEventFilter _eventFilter;
        private readonly IObserver<SubscriptionContext> _observer;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private readonly ILogger _logger;

        private readonly int _timeOutInMilliSeconds;
        private readonly string _triggerName;
        private readonly int _batchSize;
        
        public EventStoreListener(ITriggeredFunctionExecutor executor,
                                  EventProcessor eventProcessor,
                                  MessagePropagator messagePropagator,
                                  IEventStoreSubscription eventStoreSubscription,
                                  IEventFilter eventFilter,
                                  IObserver<SubscriptionContext> observer,
                                  int batchSize,
                                  int timeOutInMilliSeconds,
                                  string triggerName,
                                  ILogger logger)
        {
            _batchSize = batchSize;
            _timeOutInMilliSeconds = timeOutInMilliSeconds;
            _triggerName = triggerName;
            _logger = logger;
            _executor = executor;
            _eventProcessor = eventProcessor;
            _messagePropagator = messagePropagator;
            _eventStoreSubscription = eventStoreSubscription;
            _eventFilter = eventFilter;
            _observer = observer;
            
            _messagePropagator.Subscribe(TimeSpan.FromMilliseconds(_timeOutInMilliSeconds),
                _batchSize,ProcessEventAsync,
                OnCompleted,
                OnError,
                _eventFilter);
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _logger.LogInformation("Message propagator started.");

            return _eventStoreSubscription.StartAsync(cancellationToken, _batchSize);
        }

        private void OnError(Exception obj)
        {
            _eventStoreSubscription.Stop();
        }
        
        private void RestartSubscription()
        {
            _messagePropagator.Subscribe(TimeSpan.FromMilliseconds(_timeOutInMilliSeconds),
                _batchSize,ProcessEventAsync,
                OnCompleted,
                OnError,
                _eventFilter);
        }
        
        private void OnCompleted()
        {
            Task.Delay(_timeOutInMilliSeconds * 2).Wait(_cancellationToken);
            _observer.OnNext(new SubscriptionContext(_triggerName));
            RestartSubscription();
            _logger.LogInformation("Catchup complete.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping event listener.");
            _eventStoreSubscription.Stop();
            _logger.LogInformation("Event listener stopped.");

            return Task.FromResult(true);
        }
        
        private async Task ProcessEventAsync(IEnumerable<StreamEvent> events)
        {
            var streamEvents = events.ToList();
            await _eventProcessor.BeginProcessingEventsAsync(streamEvents, _cancellationToken).ConfigureAwait(false);
            var input = new TriggeredFunctionData
            {
                TriggerValue = new EventTriggerData(streamEvents)
            };
            var functionResult = await _executor.TryExecuteAsync(input, _cancellationToken).ConfigureAwait(false);
            await _eventProcessor.CompleteProcessingEventsAsync(streamEvents, functionResult, _cancellationToken);
        }
        
        public void Cancel()
        {
            _logger.LogInformation("Cancelling event listener.");
            _eventStoreSubscription?.Stop();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _logger.LogInformation("Disposing event listener.");
                Dispose(true);
            }
            _isDisposed = true;
            _logger.LogInformation("Event listener disposed.");
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _eventStoreSubscription = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}