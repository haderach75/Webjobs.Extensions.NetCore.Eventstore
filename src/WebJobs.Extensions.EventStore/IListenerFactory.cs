using Microsoft.Azure.WebJobs.Host.Listeners;

namespace WebJobs.Extensions.EventStore
{
    public interface IListenerFactory
    {
        IListener Create();
    }
}