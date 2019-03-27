using System;
using System.Threading.Tasks;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore
{
    public interface IMessagePropagator
    {
        void OnError(Exception exception);
        Task OnEventReceived(StreamEvent streamEvent);
        void OnCatchupCompleted();
    }
}