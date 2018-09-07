using System;
using System.Diagnostics;
using System.Linq.Expressions;
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
        protected readonly IEventStoreConnection Connection;
        protected readonly UserCredentials UserCredentials;
        private long? _lastCheckpoint;
        protected int BatchSize;
        protected readonly int MaxLiveQueueMessage;
        private CancellationToken _cancellationToken;
        
        private Subject<StreamEvent> _subject;
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

            _subject = new Subject<StreamEvent>();
        }

        public EventStoreCatchUpSubscription Subscription { get; protected set; }

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

        public virtual IDisposable Subscribe(IObserver<StreamEvent> observer)
        {
            if (OnCompletedFired)
            {
                _subject = new Subject<StreamEvent>();
            }
            return _subject.Subscribe(observer);
        }        

        private void Restart(long? position)
        {
            Logger.LogInformation("Restarting subscription...");
            
            Stop();
            StartCatchUpSubscription(position);
        }

        private Stopwatch sw = new Stopwatch();
        private int _updateCounter = 0;
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
                if(!sw.IsRunning)
                    sw.Start();
                _subject.OnNext(new StreamEvent<ResolvedEvent>(resolvedEvent));
                if (_updateCounter++ % 10000 == 0) Logger.LogDebug($"{DateTime.Now:T}: Event recieved #{_updateCounter} elasped:{sw.ElapsedMilliseconds}, avarage per 10000: {sw.ElapsedMilliseconds/ ((_updateCounter / 10000) == 0 ? 1 : (_updateCounter / 10000))}ms");
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
            return position?.CommitPosition;
        }

        protected virtual void LiveProcessingStarted(EventStoreCatchUpSubscription sub)
        {
            sw.Stop();
            Logger.LogDebug($"Catchup completed in {sw.ElapsedMilliseconds}ms");
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