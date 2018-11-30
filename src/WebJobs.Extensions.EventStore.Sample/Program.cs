using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.EventStore.Sample
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                    b.UseHostId("sample")
                        .AddAzureStorageCoreServices()
                        .AddEventStore(options =>
                        {
                            options.ConnectionString = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=20000";
                            options.Username = "admin";
                            options.Password = "changeit";
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
