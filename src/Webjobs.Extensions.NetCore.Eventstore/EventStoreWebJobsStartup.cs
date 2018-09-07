using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public class EventStoreWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddEventStore();
        }
    }
}