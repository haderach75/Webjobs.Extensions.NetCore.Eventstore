namespace WebJobs.Extensions.EventStore
{
    public interface IEventFilter
    {
        bool Filter(StreamEvent streamEvent);
    }
}