using System;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using WebJobs.Extensions.EventStore.Sample;

namespace EventsEmitter
{
    class Program
    {
        static void Main()
        {
            var connection = GetConnection();
            connection.ConnectAsync();

            Console.WriteLine("Press Enter to start emiting some test events.");
            Console.ReadLine();

            var s1 = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(10))
                .Subscribe(x => EmitEvent(connection, "stream", x));

            Console.ReadLine();
            s1.Dispose();
            connection.Close();
        }

        static void EmitEvent(IEventStoreConnection connection, string stream, long number)
        {
            Console.WriteLine($"Sending event number {number} to stream {stream} " + stream);
            connection.AppendToStreamAsync(stream, ExpectedVersion.Any, CreateEvent(number, stream));
        }

        static EventData[] CreateEvent(long number, string stream)
        {
            return new[]
            {
                new EventData(Guid.NewGuid(), typeof(Event).AssemblyQualifiedName, true,
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Event(Guid.NewGuid(), stream, DateTime.Now, number))), null)
            };
        }

        static IEventStoreConnection GetConnection()
        {
            var credentials = new UserCredentials("admin", "changeit");
            var connection =
                EventStoreConnection.Create(
                    ConnectionSettings.Create()
                        .UseConsoleLogger()
                        .SetDefaultUserCredentials(credentials),
                    new IPEndPoint(IPAddress.Loopback, 1113), "EventEmitter");
            return connection;
        }
    }

}