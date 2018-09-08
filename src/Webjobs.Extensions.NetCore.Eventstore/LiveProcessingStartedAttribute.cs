using System;
using Microsoft.Azure.WebJobs.Description;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    [Binding(TriggerHandlesReturnValue = false)]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LiveProcessingStartedAttribute : Attribute
    {
    }
}