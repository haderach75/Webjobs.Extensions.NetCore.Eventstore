using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace WebJobs.Extensions.EventStore.Impl
{
    public abstract class SubscriptionBase : IEventStoreSubscription
    {
        protected IEventStoreConnection Connection;
        protected readonly UserCredentials UserCredentials;
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly IMessagePropagator _messagePropagator;
        private readonly EventStoreOptions _options;
        private readonly string _connectionString;
        private long? _lastCheckpoint;
        protected int BatchSize;
        protected readonly int MaxLiveQueueMessage;
        private CancellationToken _cancellationToken;
        
        protected bool OnCompletedFired;
        protected bool IsStarted;
        protected readonly ILogger Logger;
       
        protected SubscriptionBase(IEventStoreConnectionFactory eventStoreConnectionFactory,
            IMessagePropagator messagePropagator,
            EventStoreOptions options,
            ILogger logger)
        {
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
            _messagePropagator = messagePropagator;
            _options = options;
            _connectionString = options.ConnectionString;
            UserCredentials = new UserCredentials(options.Username, options.Password);;
            Logger = logger;
            MaxLiveQueueMessage = options.MaxLiveQueueSize;
            
            Connection = _eventStoreConnectionFactory.Create(_connectionString, Logger);
        }

        public EventStoreCatchUpSubscription Subscription { get; protected set; }

        public virtual async Task StartAsync(CancellationToken cancellationToken, int batchSize = 200)
        {
            BatchSize = batchSize;
            _cancellationToken = cancellationToken;
            _lastCheckpoint = await _options.GetLastPositionAsync();

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
        
        public void RestartSubscription() {
            if (Subscription == null) return;
            Subscription.Stop();
            Logger.LogInformation("Restarting subscription...");
            StartCatchUpSubscription(_lastCheckpoint);
        }

        public void RestartSubscriptionWithNewConnection() {
            if (Subscription == null) return;
            Subscription.Stop();
            
            Connection = _eventStoreConnectionFactory.Create(_connectionString, Logger);
            Logger.LogInformation("Restarting subscription...");
            StartCatchUpSubscription(_lastCheckpoint);
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
                _messagePropagator.OnEventReceived(new StreamEvent<ResolvedEvent>(resolvedEvent));
                if (_updateCounter++ % 10000 == 0) Logger.LogDebug($"{DateTime.Now:T}: Event received #{_updateCounter} elapsed:{sw.ElapsedMilliseconds}, average per 10000: {sw.ElapsedMilliseconds/ ((_updateCounter / 10000) == 0 ? 1 : (_updateCounter / 10000))}ms");
                var pos = GetLong(resolvedEvent.OriginalPosition);
                if (pos != null)
                {
                    _lastCheckpoint = pos;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Exception occurred in subscription: {e.Message}");
                _messagePropagator.OnError(e);
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
                _messagePropagator.OnCatchupCompleted();
            }
        }
        
        protected void SubscriptionDropped(EventStoreCatchUpSubscription sub, SubscriptionDropReason reason, Exception ex) {
            var msg = (ex?.Message + " " + (ex?.InnerException?.Message ?? "")).TrimEnd();
            if (reason == SubscriptionDropReason.ConnectionClosed || // Will resubscribe automatically
                reason == SubscriptionDropReason.ProcessingQueueOverflow ||
                reason == SubscriptionDropReason.UserInitiated) {
                _messagePropagator.OnError(ex ?? new Exception($"Subscription dropped because {reason}: {msg}"));
                Logger.LogInformation("Subscription dropped because {Reason}: {Message}", reason, msg);
            }
            else
                Logger.LogError("Subscription dropped because {Reason}: {Message}", reason, msg);
            
            if (reason == SubscriptionDropReason.ProcessingQueueOverflow)
                RestartSubscription();
            else if (reason == SubscriptionDropReason.CatchUpError && ex is ObjectDisposedException) {
                RestartSubscriptionWithNewConnection();
            }
        }
    }
}