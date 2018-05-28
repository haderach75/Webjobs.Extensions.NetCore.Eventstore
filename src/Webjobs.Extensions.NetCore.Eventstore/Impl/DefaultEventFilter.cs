using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class DefaultEventFilter : IEventFilter
    {
        private readonly EventBuffer _eventBuffer;
        
        public DefaultEventFilter()
        {
            _eventBuffer = new EventBuffer(2000);
        }

        public DefaultEventFilter(int batchSize)
        {
            _eventBuffer = new EventBuffer(batchSize);
        }
        
        public IObservable<ResolvedEvent> Filter(IObservable<ResolvedEvent> eventStreamObservable)
        {
            return eventStreamObservable.Where(IsProcessable);
        }

        private static readonly object LockObj = new object();
        private bool IsProcessable(ResolvedEvent e)
        {
            var evt = e.Event;
            if (e.OriginalStreamId.StartsWith("$")) return false;
            if (evt.EventType == "$streamDeleted") return false;
            lock (LockObj)
            {
                if (_eventBuffer != null && _eventBuffer.Contains(evt.EventId)) return false;
                
                _eventBuffer?.Add(evt.EventId);
            }
            return true;
        }

        private class EventBuffer
        {
            private readonly Queue<Guid> _buffer;
            private readonly int _maxCapacity;
            private readonly object _lock = new object();

            public EventBuffer(int maxCapacity)
            {
                _maxCapacity = maxCapacity;
                _buffer = new Queue<Guid>(maxCapacity);
            }

            public void Add(Guid eventId)
            {
                lock (_lock)
                {
                    while (_buffer.Count >= _maxCapacity)
                        _buffer.Dequeue();
                    _buffer.Enqueue(eventId);
                }
            }

            public bool Contains(Guid eventId)
            {
                lock (_lock)
                {
                    return _buffer.Contains(eventId);
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _buffer.Clear();
                }
            }

            public int Count => _buffer.Count;
        }
    }
}