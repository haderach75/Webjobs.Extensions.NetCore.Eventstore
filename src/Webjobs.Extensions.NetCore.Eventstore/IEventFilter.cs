using EventStore.ClientAPI;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IEventFilter
    {
        bool IsProcessable(ResolvedEvent e);
    }
}