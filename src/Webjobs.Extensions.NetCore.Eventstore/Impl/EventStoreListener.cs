using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private IEventStoreSubscription _eventStoreSubscription;
        private readonly IEventFilter _eventFilter;
        private readonly IObserver<IEnumerable<ResolvedEvent>> _observer;
        private readonly TraceWriter _trace;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observable;

        public int TimeOutInMilliSeconds { get; }
        public int BatchSize { get; }
        
        public EventStoreListener(ITriggeredFunctionExecutor executor, 
                                  IEventStoreSubscription eventStoreSubscription,
                                  IEventFilter eventFilter,
                                  IObserver<IEnumerable<ResolvedEvent>> observer,
                                  int batchSize,
                                  int timeOutInMilliSeconds,
                                  TraceWriter trace)
        {
            BatchSize = batchSize;
            TimeOutInMilliSeconds = timeOutInMilliSeconds;
            _executor = executor;
            _eventStoreSubscription = eventStoreSubscription;
            _eventFilter = eventFilter;
            _observer = observer;
           _trace = trace;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _observable = GetObservable()
                .Subscribe(ProcessEvent, OnCompleted);
            
            _trace.Info("Observable subscription started.");

            _eventStoreSubscription.Start(cancellationToken, BatchSize);
            return Task.FromResult(true);
        }

        private IDisposable RestartSubscription()
        {
            _trace.Info("Restarting observable subscription.");
            _observable = GetObservable().Catch(GetObservable()).Subscribe(ProcessEvent);
            return _observable;
        }

        private IObservable<IEnumerable<ResolvedEvent>> GetObservable()
        {
            var observable = (IObservable<ResolvedEvent>) _eventStoreSubscription;
            if (_eventFilter != null)
                observable = _eventFilter.Filter(observable);
            
            return observable
                .Buffer(TimeSpan.FromMilliseconds(TimeOutInMilliSeconds), BatchSize)
                .Where(buffer => buffer.Any());
        }
        
        private void OnCompleted()
        {
            _observable = RestartSubscription();
            _observer.OnCompleted();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _trace.Info("Stopping event listener.");
            _observable.Dispose();
            _eventStoreSubscription.Stop();
            _trace.Info("Event listener stopped.");

            return Task.FromResult(true);
        }

        private void ProcessEvent(IEnumerable<ResolvedEvent> events)
        {
            TriggeredFunctionData input = new TriggeredFunctionData
            {
                TriggerValue = new EventStoreTriggerValue(events)
            };
            _executor.TryExecuteAsync(input, _cancellationToken).Wait();
        }
        
        public void Cancel()
        {
            _trace.Info("Cancelling event listener.");
            _observable?.Dispose();
            _eventStoreSubscription?.Stop();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _trace.Info("Disposing event listener.");
                Dispose(true);
            }
            _isDisposed = true;
            _trace.Info("Event listener disposed.");
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