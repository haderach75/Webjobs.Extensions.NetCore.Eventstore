# Azure Webjobs Eventstore extension

This repo contains binding extension for the Eventstore (https://github.com/EventStore/EventStore).

## Getting Started

When creating the Jobhost use the following extension method to bind the triggers.

```csharp
config.UseEventStore(new EventStoreConfig
{
    ConnectionString = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=20000",
    Username = "admin",
    Password = "changeit",
    LastPosition = new Position(0,0),
    MaxLiveQueueSize = 500
});
```

In the eventstore configuration has options to override most factories used during the startup of the jobhost. The event store subscription is an observable stream which can be prefiltered with reactive extensions.

```csharp
config.UseEventStore(new EventStoreConfig
{
    ...
    EventFilter = new MyEventFilter()
    ...
});

public class MyEventFilter : IEventFilter
{   
    /// <summary>
    /// Filter $stat stream and deleted events.
    /// </summary>                
    public IObservable<ResolvedEvent> Filter(IObservable<ResolvedEvent> eventStreamObservable)
    {
        return eventStreamObservable.Where(e => !e.OriginalStreamId.StartsWith("$") && e.Event.EventType != "$streamDeleted");
    }
}
```

The event trigger can subscribe to all stream or an specific stream by name. When the subscription reaches the current position the LiveProcessingStarted trigger is fired. The trigger fires when the batch buffer has filled up or the timeout has elapsed. 

```csharp        
[Singleton(Mode = SingletonMode.Listener)]
public void ProcessQueueMessage([EventTrigger(BatchSize = 10, TimeOutInMilliSeconds = 20)] IEnumerable<ResolvedEvent> events)
{
    //Handle the delivered events
}

[Disable(WebJobDisabledSetting)]
public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
{
    //Handle the swap from catchup to live mode
}
```

## Authors

* **Michael Hultman* - *Initial work* -

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Collector bank and the savings team.
