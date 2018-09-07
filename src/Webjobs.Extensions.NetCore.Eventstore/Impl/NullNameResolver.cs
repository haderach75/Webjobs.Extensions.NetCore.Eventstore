using Microsoft.Azure.WebJobs;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class NullNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return name;
        }
    }
}