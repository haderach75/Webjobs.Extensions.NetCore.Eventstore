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
        protected readonly int MaxLiveQueueMessage;
        protected readonly Stopwatch CatchupWatch = new Stopwatch();
        protected bool IsCatchingUp;
        protected long CatchupEventCount;
        protected Position LastAllPosition;
        protected int BatchSize;
        protected bool OnCompletedFired;
        private bool _isStarted;
        protected readonly ILogger Logger;
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactoryFactory;
        private readonly IMessagePropagator _messagePropagator;
        private readonly EventStoreOptions _options;
        private readonly string _connectionString;
        private long? _lastCheckpoint;
        
        private CancellationToken _cancellationToken;
       
        protected SubscriptionBase(IEventStoreConnectionFactory eventStoreConnectionFactoryFactory,
            IMessagePropagator messagePropagator,
            EventStoreOptions options,
            ILogger logger)
        {
            _eventStoreConnectionFactoryFactory = eventStoreConnectionFactoryFactory;
            _messagePropagator = messagePropagator;
            _options = options;
            _connectionString = options.ConnectionString;
            UserCredentials = new UserCredentials(options.Username, options.Password);;
            Logger = logger;
            MaxLiveQueueMessage = options.MaxLiveQueueSize;
        }

        public EventStoreCatchUpSubscription Subscription { get; protected set; }

        public virtual async Task StartAsync(CancellationToken cancellationToken, int batchSize = 200)
        {
            BatchSize = batchSize;
            _cancellationToken = cancellationToken;
            _lastCheckpoint = await _options.GetLastPositionAsync();

            if (!_isStarted)
            {
                Connection = _eventStoreConnectionFactoryFactory.Create(_connectionString, Logger);
                
                await Connection.ConnectAsync();
                StartCatchUpSubscription(_lastCheckpoint);
                _isStarted = true;
            }
        }
        
        protected abstract void StartCatchUpSubscription(long? startPosition);
        
        private int _lastPercent = -1;
        private DateTime _lastTime = DateTime.MinValue;
        private void ReportRebuildProgress(Position pos) {
            if (IsCatchingUp && LastAllPosition.CommitPosition > 0) {
                var percent = (int)(pos.CommitPosition * 100.0 / LastAllPosition.CommitPosition);
                if (percent > _lastPercent || DateTime.Now >= _lastTime.AddMinutes(5)) {
                    _lastPercent = percent;
                    _lastTime = DateTime.Now;
                    Logger.LogInformation("Rebuild is {RebuildPercentage}% complete, rebuild time {ElapsedTime}", percent, CatchupWatch.Elapsed);
                }
            }
        }

        private void IncrementCatchupCount() {
            if (IsCatchingUp) CatchupEventCount++;
        }
        
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
            _isStarted = false;
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
            
            Connection = _eventStoreConnectionFactoryFactory.Create(_connectionString, Logger);
            Logger.LogInformation("Restarting subscription...");
            StartCatchUpSubscription(_lastCheckpoint);
        }
        
        protected virtual Task EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent resolvedEvent)
        {
            if (_cancellationToken != CancellationToken.None && _cancellationToken.IsCancellationRequested) {
                Logger.LogInformation("Cancellation requested, stopping subscription...");
                sub.Stop();
                return Task.CompletedTask;
            }
            
            var evt = resolvedEvent.Event;
            var pos = resolvedEvent.OriginalPosition ?? Position.Start;
            if (Subscription != sub) return Task.CompletedTask;; // Not the current subscription
            try
            {
                _messagePropagator.OnEventReceived(new StreamEvent<ResolvedEvent>(resolvedEvent));
                IncrementCatchupCount();
                ReportRebuildProgress(pos);
                _lastCheckpoint = GetLong(pos);
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error occurred when processing event {EventType} with id {EventId} from the stream {EventStreamId}", evt.EventType, evt.EventId, evt.EventStreamId);
                _messagePropagator.OnError(ex);
                throw;
            }
            return Task.CompletedTask;
        }

        private long? GetLong(Position? position)
        {
            return position?.CommitPosition;
        }
        
        protected virtual void LiveProcessingStarted(EventStoreCatchUpSubscription obj) {
            CatchupWatch.Stop();
            if (IsCatchingUp)
            {
                var eventsPerSecond = CatchupWatch.Elapsed.TotalSeconds > 0.0 ? CatchupEventCount / CatchupWatch.Elapsed.TotalSeconds : double.NaN;
                Logger.LogInformation($"Live processing, catching up took {CatchupWatch.Elapsed}, processing {CatchupEventCount} events ({eventsPerSecond:N2} e/s).");
            }
            else {
                Logger.LogInformation($"Live processing started.");
            }
            LastAllPosition = new Position();
            IsCatchingUp = false;
            CatchupEventCount = 0;
            CatchupWatch.Reset();
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