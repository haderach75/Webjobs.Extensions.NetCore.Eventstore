using System.Threading.Tasks;

namespace WebJobs.Extensions.EventStore.Sample
{
    public interface IEventPublisher<T>
    {
        Task PublishAsync(T item);
    }
}