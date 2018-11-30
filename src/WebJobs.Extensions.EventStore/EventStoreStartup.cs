using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

namespace WebJobs.Extensions.EventStore
{
    public class EventStoreStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddEventStore();
        }
    }
}