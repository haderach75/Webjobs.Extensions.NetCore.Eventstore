using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public abstract class EventStoreStreamSubscriptionObservableBase : IEventStoreSubscription
    {
        protected EventStoreCatchUpSubscription Subscription;
        protected readonly Lazy<IEventStoreConnection> Connection;
        protected readonly UserCredentials UserCredentials;
        protected readonly TraceWriter Trace;
        private long? _lastCheckpoint;
        protected int BatchSize;
        protected readonly int MaxLiveQueueMessage;
        private CancellationToken _cancellationToken;
        
        private Subject<ResolvedEvent> _subject;
        private IConnectableObservable<ResolvedEvent> _observable;
        protected bool OnCompletedFired;
        protected bool IsStarted;
        
        protected EventStoreStreamSubscriptionObservableBase(Lazy<IEventStoreConnection> connection, 
            long? lastCheckpoint,
            int maxLiveQueueMessage,
            UserCredentials userCredentials,
            TraceWriter trace)
        {
            _lastCheckpoint = lastCheckpoint;
            UserCredentials = userCredentials;
            Trace = trace;
            Connection = connection;
            MaxLiveQueueMessage = maxLiveQueueMessage;

            _subject = new Subject<ResolvedEvent>();
            _observable = _subject.Publish();
        }
        
        public virtual void Start(CancellationToken cancellationToken, int batchSize = 200)
        {
            BatchSize = batchSize;
            _cancellationToken = cancellationToken;
            if(!IsStarted)
                StartCatchUpSubscription(_lastCheckpoint);
        }

        protected abstract void StartCatchUpSubscription(long? startPosition);
        
        public virtual void Stop()
        {
            if (Subscription == null) return;
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                Trace.Info($"Stopping subscription with timeout {timeout}...");
                Subscription?.Stop(timeout);
                Trace.Info("Subscription stopped");
            }
            catch (TimeoutException)
            {
                Trace.Warning("The subscription did not stop within the specified time.");
            }
            IsStarted = false;
        }

        public virtual IDisposable Subscribe(IObserver<ResolvedEvent> observer)
        {
            if (OnCompletedFired)
            {
                _subject = new Subject<ResolvedEvent>();
                _observable = _subject.Publish();
            }
            return _subject.Subscribe(observer);
        }

        public virtual IDisposable Connect()
        {
            return _observable.Connect();
        }

        public virtual void Restart(long? position)
        {
            Trace.Info("Restarting subscription...");
            
            Stop();
            StartCatchUpSubscription(position);
        }

        protected virtual Task EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent resolvedEvent)
        {
            if (_cancellationToken != CancellationToken.None && _cancellationToken.IsCancellationRequested)
            {
                System.Diagnostics.Trace.TraceInformation("Cancellation requested, stopping subscription...");
                sub.Stop();
                return Task.CompletedTask;
            }

            try
            {
                _subject.OnNext(resolvedEvent);
                var pos = GetLong(resolvedEvent.OriginalPosition);
                if (pos != null)
                {
                    _lastCheckpoint = pos;
                }
            }
            catch (Exception e)
            {
                Trace.Error($"Exception occured in subsciption: {e.Message}");
                _subject.OnError(e);
            }
            return Task.CompletedTask;
        }

        private long? GetLong(Position? position)
        {
            return position.HasValue ? position.Value.CommitPosition : (long?) null;
        }

        protected virtual void LiveProcessingStarted(EventStoreCatchUpSubscription sub)
        {
            if (!OnCompletedFired)
            {
                OnCompletedFired = true;
                _subject.OnCompleted();
            }
        }

        protected virtual void SubscriptionDropped(EventStoreCatchUpSubscription sub, SubscriptionDropReason reason, Exception e)
        {
            var msg = (e?.Message + " " + (e?.InnerException?.Message ?? "")).TrimEnd();
            Trace.Warning($"Subscription dropped because {reason}: {msg}");
            if (reason == SubscriptionDropReason.ProcessingQueueOverflow)
                Restart(_lastCheckpoint);
        }
    }
}