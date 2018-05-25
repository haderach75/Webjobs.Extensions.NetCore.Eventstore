using System;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EventTriggerAttribute : Attribute
    {
        /// <summary>
        /// If batchSize is not exceeded within this timeout trigger is fired.
        /// Default value is 100
        /// </summary>
        public int TimeOutInMilliSeconds { get; set; } = 100;

        /// <summary>
        /// Max batch size before a trigger is fired for event store subscription.
        /// Default value is 1024
        /// </summary>
        public int BatchSize { get; set; } = 1024;
        
        /// <summary>
        /// The name of the stream to subscribe to.
        /// </summary>
        public string Stream { get; set; }
    }
}
