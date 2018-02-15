using Microsoft.Azure.WebJobs;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class NameResolver : INameResolver
    {
        public NameResolver()
        {
            
        }
        
        public string Resolve(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}