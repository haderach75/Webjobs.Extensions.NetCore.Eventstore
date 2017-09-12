using System.Reactive.Subjects;
using System.Threading;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscription : IConnectableObservable<ResolvedEvent>
    {
        void Start(CancellationToken token, int batchSize = 200);
        void Restart();
        void Stop();
    }
}