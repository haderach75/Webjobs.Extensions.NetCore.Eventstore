using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Webjobs.Extensions.NetCore.Eventstore.Impl;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventStoreSubscription : IObservable<StreamEvent>
    {
        Task StartAsync(CancellationToken token, int batchSize = 200);
        void Stop();
    }
}