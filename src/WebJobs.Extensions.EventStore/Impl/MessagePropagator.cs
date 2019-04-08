using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI;
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
            Action<Exception> onError = null)
        {
            _onCompleted = onCompleted;
            _onError = onError;

            _semaphore = new SemaphoreSlim(capacity, capacity);
            
            var options = new DataflowLinkOptions {PropagateCompletion = true};
            _bufferBlock = CreateBuffer(timeout, capacity);
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
            });
            _bufferBlock.LinkTo(_outputBlock, options);
        }

        private IPropagatorBlock<StreamEvent,IList<StreamEvent>> CreateBuffer(TimeSpan timeSpan, int capacity)
        {
            var options = new DataflowLinkOptions {PropagateCompletion = true};
            var inBlock = new BufferBlock<StreamEvent>();
            
            var batchBlock = new BatchBlock<StreamEvent>(capacity, new GroupingDataflowBlockOptions { Greedy = true });
            
            var timer = new Timer(_ => batchBlock.TriggerBatch());
            var timingBlock = new TransformBlock<StreamEvent, StreamEvent>(streamEvent =>
            {
                timer.Change(timeSpan, Timeout.InfiniteTimeSpan);
                return streamEvent;
            });

            inBlock.LinkTo(timingBlock, options);

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

        public async Task StopAsync()
        {
            _bufferBlock.Complete();
            await _outputBlock.Completion;
        }
    }
}