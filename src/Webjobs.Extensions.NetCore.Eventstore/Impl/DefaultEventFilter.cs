using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class DefaultEventFilter : IEventFilter
    {
        private readonly EventBuffer _eventBuffer;

        public DefaultEventFilter(int batchSize)
        {
            _eventBuffer = new EventBuffer(batchSize + 28);
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
                if (_eventBuffer != null && _eventBuffer.Contains(evt.EventId))
                {
                    Trace.TraceWarning($"Duplicate event {evt.EventType} {evt.EventId} in stream {evt.EventNumber}@{evt.EventStreamId}. Skipping processing.");
                    return false;
                }
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