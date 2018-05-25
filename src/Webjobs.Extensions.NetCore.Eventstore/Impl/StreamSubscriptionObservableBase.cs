using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public abstract class SubscriptionObservableBase : IEventStoreSubscription
    {
        protected EventStoreCatchUpSubscription Subscription;
        protected readonly IEventStoreConnection Connection;
        protected readonly UserCredentials UserCredentials;
        private long? _lastCheckpoint;
        protected int BatchSize;
        protected readonly int MaxLiveQueueMessage;
        private CancellationToken _cancellationToken;
        
        private Subject<ResolvedEvent> _subject;
        protected bool OnCompletedFired;
        protected bool IsStarted;
        protected readonly ILogger Logger;
        
        protected SubscriptionObservableBase(IEventStoreConnection connection, 
            long? lastCheckpoint,
            int maxLiveQueueMessage,
            UserCredentials userCredentials, 
            ILogger logger)
        {
            _lastCheckpoint = lastCheckpoint;
            UserCredentials = userCredentials;
            Logger = logger;
            Connection = connection;
            MaxLiveQueueMessage = maxLiveQueueMessage;

            _subject = new Subject<ResolvedEvent>();
        }
        
        public virtual async Task StartAsync(CancellationToken cancellationToken, int batchSize = 200)
        {
            BatchSize = batchSize;
            _cancellationToken = cancellationToken;
            if (!IsStarted)
            {
                await Connection.ConnectAsync();
                StartCatchUpSubscription(_lastCheckpoint);
            }
        }

        protected abstract void StartCatchUpSubscription(long? startPosition);
        
        public virtual void Stop()
        {
            if (Subscription == null) return;
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                Logger.LogInformation($"Stopping subscription with timeout {timeout}...");
                Subscription?.Stop(timeout);
                Logger.LogInformation("Subscription stopped");
            }
            catch (TimeoutException)
            {
                Logger.LogWarning("The subscription did not stop within the specified time.");
            }
            IsStarted = false;
        }

        public virtual IDisposable Subscribe(IObserver<ResolvedEvent> observer)
        {
            if (OnCompletedFired)
            {
                _subject = new Subject<ResolvedEvent>();
            }
            return _subject.Subscribe(observer);
        }        

        private void Restart(long? position)
        {
            Logger.LogInformation("Restarting subscription...");
            
            Stop();
            StartCatchUpSubscription(position);
        }

        protected virtual Task EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent resolvedEvent)
        {
            if (_cancellationToken != CancellationToken.None && _cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("Cancellation requested");
                Stop();
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
                Logger.LogError($"Exception occured in subsciption: {e.Message}");
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
            Logger.LogWarning($"Subscription dropped because {reason}: {msg}");
            if (reason == SubscriptionDropReason.ProcessingQueueOverflow)
                Restart(_lastCheckpoint);
        }
    }
}