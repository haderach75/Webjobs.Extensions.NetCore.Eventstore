namespace WebJobs.Extensions.EventStore.Impl
{
    public class NullEventFilter : IEventFilter
    {
        public bool Filter(StreamEvent streamEvent)
        {
            return true;
        }
    }
}