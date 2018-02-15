using System;
using System.Threading;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscription : IObservable<ResolvedEvent>
    {
        void Start(CancellationToken token, int batchSize = 200);
        void Restart(long? position);
        void Stop();
    }
}