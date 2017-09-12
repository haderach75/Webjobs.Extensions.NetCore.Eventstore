using System;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EventTriggerAttribute : Attribute
    {
        /// <summary>
        /// If batchSize is not exceeded within this timeout trigger is fired.
        /// Default value is 50
        /// </summary>
        public int TimeOutInMilliSeconds { get; set; }
        /// <summary>
        /// Max batch size before a trigger is fired for event store subscription.
        /// Default value is 100
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public EventTriggerAttribute()
        {
            TimeOutInMilliSeconds = 50;
            BatchSize = 100;
        }
    }
}
