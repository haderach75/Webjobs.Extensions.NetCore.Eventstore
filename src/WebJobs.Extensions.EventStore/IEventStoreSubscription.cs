using System;
using System.Threading;
using System.Threading.Tasks;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore
{
    public interface IEventStoreSubscription : IObservable<StreamEvent>
    {
        Task StartAsync(CancellationToken token, int batchSize = 200);
        void Stop();
    }
}