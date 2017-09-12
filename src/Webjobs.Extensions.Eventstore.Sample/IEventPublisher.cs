namespace Webjobs.Extensions.Eventstore.Sample
{
    public interface IEventPublisher<T>
    {
        void Publish(T item);
    }
}