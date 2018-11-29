using Microsoft.Azure.WebJobs;

namespace WebJobs.Extensions.EventStore.Impl
{
    public class NullNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return name;
        }
    }
}