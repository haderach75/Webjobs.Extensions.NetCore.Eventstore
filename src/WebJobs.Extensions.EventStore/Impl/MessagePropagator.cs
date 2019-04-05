using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class MessagePropagator : IMessagePropagator
    {
        private readonly ILogger<MessagePropagator> _logger;
        private Action _onCompleted;
        private Action<Exception> _onError;
        private IPropagatorBlock<StreamEvent, IList<StreamEvent>> _bufferBlock;
        private SemaphoreSlim _semaphore;
        private ActionBlock<IList<StreamEvent>> _outputBlock;

        public MessagePropagator(ILogger<MessagePropagator> logger)
        {
            _logger = logger;
        }

        public void Subscribe(TimeSpan timeout,
            int capacity,
            Func<IEnumerable<StreamEvent>, Task> onNext,
            Action onCompleted = null,
            Action<Exception> onError = null,
            IEventFilter eventFilter = null)
        {
            _onCompleted = onCompleted;
            _onError = onError;

            _semaphore = new SemaphoreSlim(capacity, capacity);
            
            var options = new DataflowLinkOptions {PropagateCompletion = true};
            _bufferBlock = CreateBuffer(timeout, capacity, eventFilter);
            
            _outputBlock = new ActionBlock<IList<StreamEvent>>(async m =>
                {
                    try
                    {
                        await onNext(m);
                    }
                    finally
                    {
                        _semaphore.Release(m.Count);
                    }
                }
            );
            _bufferBlock.LinkTo(_outputBlock, options);
        }

        private IPropagatorBlock<StreamEvent,IList<StreamEvent>> CreateBuffer(TimeSpan timeSpan, int capacity, IEventFilter eventFilter)
        {
            var options = new DataflowLinkOptions {PropagateCompletion = true};
            var inBlock = new BufferBlock<StreamEvent>();
            
            var batchBlock = new BatchBlock<StreamEvent>(capacity, new GroupingDataflowBlockOptions { Greedy = true });
            
            var timer = new Timer(_ => batchBlock.TriggerBatch());
            
            Func<StreamEvent, StreamEvent> resetTimerIdentity = value =>
            {
                timer.Change(timeSpan, Timeout.InfiniteTimeSpan);
                return value;
            };
            
            var timingBlock = new TransformBlock<StreamEvent, StreamEvent>(resetTimerIdentity);
            
            var outObserver=timingBlock.AsObserver();
            inBlock.AsObservable()
                .ApplyFilter(eventFilter)
                .Subscribe(outObserver);
            
            //inBlock.LinkTo(timingBlock, options);
            timingBlock.LinkTo(batchBlock, options);
            
            return DataflowBlock.Encapsulate(inBlock, batchBlock);
        }
        
        public void OnError(Exception exception)
        {
            _onError?.Invoke(exception);
        }
        
        public async Task OnEventReceived(StreamEvent streamEvent)
        {
            _semaphore.Wait();
            await _bufferBlock.SendAsync(streamEvent);
        }

        public void OnCatchupCompleted()
        {
            _bufferBlock.Complete();
            _outputBlock.Completion.Wait();
           _onCompleted?.Invoke();
        }
    }
}