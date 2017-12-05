using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreCatchUpSubscriptionObservable : IEventStoreSubscription
    {
        private EventStoreAllCatchUpSubscription _subscription;
        private readonly Lazy<IEventStoreConnection> _connection;
        private readonly UserCredentials _userCredentials;
        private readonly TraceWriter _trace;
        private Position? _lastCheckpoint;
        private int _batchSize;
        private readonly int _maxLiveQueueMessage;
        private CancellationToken _cancellationToken;
        
        private Subject<ResolvedEvent> _subject;
        private IConnectableObservable<ResolvedEvent> _observable;
        private bool _onCompletedFired;
        private bool _isStarted;
        
        public EventStoreCatchUpSubscriptionObservable(Lazy<IEventStoreConnection> connection,
            Position? lastCheckpoint,
            int maxLiveQueueMessage,
            UserCredentials userCredentials,
            TraceWriter trace)
        {
            _lastCheckpoint = lastCheckpoint;
            _userCredentials = userCredentials;
            _trace = trace;
            _connection = connection;
            _maxLiveQueueMessage = maxLiveQueueMessage;

            _subject = new Subject<ResolvedEvent>();
            _observable = _subject.Publish();
        }
        
        public void Start(CancellationToken cancellationToken, int batchSize = 200)
        {
            _batchSize = batchSize;
            _cancellationToken = cancellationToken;
            if(!_isStarted)
                StartCatchUpSubscription(_lastCheckpoint);
        }
        
        private void StartCatchUpSubscription(Position? startPosition)
        {
            _onCompletedFired = false;
            _isStarted = true;
            var settings = new CatchUpSubscriptionSettings(_maxLiveQueueMessage, _batchSize, true, false);
            _subscription = _connection.Value.SubscribeToAllFrom(
                startPosition ?? AllCheckpoint.AllStart,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                _userCredentials);
            
            _trace.Info($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
        }
        
        public void Stop()
        {
            if (_subscription == null) return;
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                _trace.Info($"Stopping subscription with timeout {timeout}...");
                _subscription?.Stop(timeout);
                _trace.Info("Subscription stopped");
            }
            catch (TimeoutException)
            {
                _trace.Warning("The subscription did not stop within the specified time.");
            }
            _isStarted = false;
        }

        public IDisposable Subscribe(IObserver<ResolvedEvent> observer)
        {
            if (_onCompletedFired)
            {
                _subject = new Subject<ResolvedEvent>();
                _observable = _subject.Publish();
            }
            return _subject.Subscribe(observer);
        }

        public IDisposable Connect()
        {
            return _observable.Connect();
        }

        public void Restart(Position? position)
        {
            _trace.Info("Restarting subscription...");
            
            Stop();
            StartCatchUpSubscription(position);
        }

        private Task EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent resolvedEvent)
        {
            if (_cancellationToken != CancellationToken.None && _cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Cancellation requested, stopping subscription...");
                sub.Stop();
                return Task.CompletedTask;
            }

            _subject.OnNext(resolvedEvent);
            var pos = resolvedEvent.OriginalPosition;
            if (pos != null)
            {
                _lastCheckpoint = pos;
            }
            return Task.CompletedTask;
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription sub)
        {
            if (!_onCompletedFired)
            {
                _onCompletedFired = true;
               _subject.OnCompleted();
            }
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription sub, SubscriptionDropReason reason, Exception e)
        {
            var msg = (e?.Message + " " + (e?.InnerException?.Message ?? "")).TrimEnd();
            _trace.Warning($"Subscription dropped because {reason}: {msg}");
            if (reason == SubscriptionDropReason.ProcessingQueueOverflow)
                Restart(_lastCheckpoint);
        }
    }
}