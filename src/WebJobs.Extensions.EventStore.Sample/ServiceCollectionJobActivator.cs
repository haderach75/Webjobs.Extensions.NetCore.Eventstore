using System;
using Microsoft.Azure.WebJobs.Host;

namespace WebJobs.Extensions.EventStore.Sample
{
    public class ServiceCollectionJobActivator : IJobActivator
    {
        private readonly IServiceProvider _serviceProvider;
        
        public ServiceCollectionJobActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public T CreateInstance<T>()
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }
    }
}