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

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private IEventStoreSubscription _eventStoreSubscription;
        private readonly IEventFilter _eventFilter;
        private readonly IObserver<SubscriptionContext> _observer;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observable;
        private readonly ILogger _logger;

        private readonly int _timeOutInMilliSeconds;
        private readonly string _triggerName;
        private readonly int _batchSize;
        
        public EventStoreListener(ITriggeredFunctionExecutor executor, 
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
            _eventStoreSubscription = eventStoreSubscription;
            _eventFilter = eventFilter;
            _observer = observer;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _observable = GetObservable()
                .Subscribe(ProcessEvent, OnCompleted);
            
            _logger.LogInformation("Observable subscription started.");

            return _eventStoreSubscription.StartAsync(cancellationToken, _batchSize);
        }

        private IDisposable RestartSubscription()
        {
            _logger.LogInformation("Restarting observable subscription.");
            _observable = GetObservable().Catch(GetObservable()).Subscribe(ProcessEvent);
            return _observable;
        }

        private IObservable<IEnumerable<StreamEvent>> GetObservable()
        {
            var observable = (IObservable<StreamEvent>) _eventStoreSubscription;
            if (_eventFilter != null)
                observable = _eventFilter.Filter(observable);
            
            return observable
                .Buffer(TimeSpan.FromMilliseconds(_timeOutInMilliSeconds), _batchSize)
                .Where(buffer => buffer.Any());
        }
        
        private void OnCompleted()
        {
            _observable = RestartSubscription();
            _observer.OnNext(new SubscriptionContext(_triggerName));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping event listener.");
            _observable.Dispose();
            _eventStoreSubscription.Stop();
            _logger.LogInformation("Event listener stopped.");

            return Task.FromResult(true);
        }

        private void ProcessEvent(IEnumerable<StreamEvent> events)
        {
            TriggeredFunctionData input = new TriggeredFunctionData
            {
                TriggerValue = new EventTriggerData(events)
            };
            _executor.TryExecuteAsync(input, _cancellationToken).Wait();
        }
        
        public void Cancel()
        {
            _logger.LogInformation("Cancelling event listener.");
            _observable?.Dispose();
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
                Cancel();
                _eventStoreSubscription = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}