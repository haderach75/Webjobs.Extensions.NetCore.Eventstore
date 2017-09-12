using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IListenerFactory
    {
        IListener Create();
    }
}