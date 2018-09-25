using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Webjobs.Extensions.Eventstore.Sample;
using Webjobs.Extensions.NetCore.Eventstore;

namespace Webjobs.Extensions.Eventstore.Sample
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                    b.UseHostId("ecad61-62cf-47f4-93b4-6efcded6")
                        .AddAzureStorageCoreServices()
                        .AddEventStore(options =>
                        {
                            options.ConnectionString = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=20000";
                            options.Username = "admin";
                            options.Password = "changeit";
                            options.LastPosition = 0;
                            options.MaxLiveQueueSize = 10000;
                        });
                })
                .AddServices()
                .ConfigureLogging((context, b) =>
                {
                    b.SetMinimumLevel(LogLevel.Information);
                    b.AddConsole();
                })
                .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
