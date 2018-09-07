using System.Threading.Tasks;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public interface IEventPublisher<T>
    {
        Task PublishAsync(T item);
    }
}