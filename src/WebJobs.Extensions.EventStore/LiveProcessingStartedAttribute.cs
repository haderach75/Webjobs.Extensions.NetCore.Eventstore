using System;
using Microsoft.Azure.WebJobs.Description;

namespace WebJobs.Extensions.EventStore
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LiveProcessingStartedAttribute : Attribute
    {
    }
}