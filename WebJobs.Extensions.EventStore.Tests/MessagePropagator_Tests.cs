using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using WebJobs.Extensions.EventStore.Impl;

namespace WebJobs.Extensions.EventStore.Tests
{
    public class MessagePropagator_Tests
    {
        [Test]
        public async Task MessagePropagator_OnCompleted_All_Is_Transmitted()
        {
            int messageCount = 20;
            var receivedItems = new List<StreamEvent>();
            
            //Build the pipeline
            var sut = new MessagePropagator(new NullLogger<MessagePropagator>(), new NullEventFilter());
            
            sut.Subscribe(TimeSpan.FromMilliseconds(1), 10, (e) =>
                {
                    receivedItems.AddRange(e);
                    return Task.CompletedTask;
                }, 
                () => Console.WriteLine($"Finished processing messages on thread {Thread.CurrentThread.ManagedThreadId}"));
            
            //Start the messages
            Console.WriteLine($"Starting on thread {Thread.CurrentThread.ManagedThreadId}");

            Console.WriteLine($"Sending {messageCount} messages on thread {Thread.CurrentThread.ManagedThreadId}");
            for (int i=0;i<messageCount;i++)
            {
                await sut.OnEventReceived(new StreamEvent<string>(i.ToString()));
            }
            
            Console.WriteLine($"Finished sending {messageCount} messages");
            sut.OnCatchupCompleted();
            Console.WriteLine($"Received {receivedItems.Count} messages");
            
            Assert.AreEqual(messageCount, receivedItems.Count);
        }
        
        static Task Print(IEnumerable<string> items)
        {
            var receivedItems = items.ToList();
            if (!receivedItems.Any())
                return Task.CompletedTask;
            
            var line = String.Join(",", receivedItems);
            Console.WriteLine($"{line} ({receivedItems.Count}) on thread {Thread.CurrentThread.ManagedThreadId}");
            return Task.CompletedTask;
        }
    }
}