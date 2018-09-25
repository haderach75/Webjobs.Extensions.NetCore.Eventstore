using System;
using Microsoft.Azure.WebJobs.Description;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LiveProcessingStartedAttribute : Attribute
    {
    }
}