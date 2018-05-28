using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscription : IObservable<ResolvedEvent>
    {
        EventStoreCatchUpSubscription Subscription { get; }
        Task StartAsync(CancellationToken token, int batchSize = 200);
        void Stop();
    }
}