using Microsoft.Azure.WebJobs.Host;
using SimpleInjector;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class SimpleInjectorJobActivator : IJobActivator
    {
        private readonly Container _container;

        public SimpleInjectorJobActivator(Container container)
        {
            _container = container;
        }
        public T CreateInstance<T>()
        {
            return (T)_container.GetInstance(typeof(T));
        }
    }
}