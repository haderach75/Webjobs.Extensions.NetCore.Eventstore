using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class MessagePropagator : IMessagePropagator
    {
        private Func<IEnumerable<StreamEvent>, Task> _onNext;
        private Action _onCompleted;
        private Action<Exception> _onError;
        private IPropagatorBlock<StreamEvent, IEnumerable<StreamEvent>> _bufferBlock;
        
        public void Subscribe(TimeSpan timeout, 
            int buffer, 
            Func<IEnumerable<StreamEvent>, Task> onNext, 
            Action onCompleted = null, 
            Action<Exception> onError = null,
            IEventFilter eventFilter = null)
        {
            _onNext = onNext;
            _onCompleted = onCompleted;
            _onError = onError;
            
            _bufferBlock = CreateBuffer(timeout, buffer, eventFilter);
            
            var options = new DataflowLinkOptions { PropagateCompletion = true };
            var outputBlock = new ActionBlock<IEnumerable<StreamEvent>>(_onNext);
            _bufferBlock.LinkTo(outputBlock, options);
        }
        
        private static IPropagatorBlock<StreamEvent,IList<StreamEvent>> CreateBuffer(TimeSpan timeSpan,int count, IEventFilter eventFilter)
        {
            var inBlock = new BufferBlock<StreamEvent>();
            var outBlock = new BufferBlock<IList<StreamEvent>>();

            var outObserver=outBlock.AsObserver();
            inBlock.AsObservable()
                .ApplyFilter(eventFilter)
                .Buffer(timeSpan, count)
                .Where(b => b.Count > 0)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(outObserver);

            return DataflowBlock.Encapsulate(inBlock, outBlock);
        }
        
        public void OnError(Exception exception)
        {
            _onError?.Invoke(exception);
        }

        public async Task OnEventReceived(StreamEvent streamEvent)
        {
            await _bufferBlock.SendAsync(streamEvent);
        }

        public void OnCatchupCompleted()
        {
            _bufferBlock.Complete();
            _bufferBlock.Completion.Wait();
            _onCompleted?.Invoke();
        }
    }
}