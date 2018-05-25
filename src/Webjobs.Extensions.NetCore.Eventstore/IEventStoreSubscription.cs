using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscription : IObservable<ResolvedEvent>
    {
        Task StartAsync(CancellationToken token, int batchSize = 200);
        void Stop();
    }
}