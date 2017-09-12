using System;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LiveProcessingStartedAttribute : Attribute
    {
        
    }
}