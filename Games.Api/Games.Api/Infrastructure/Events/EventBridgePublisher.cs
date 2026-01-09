using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using System.Text.Json;

namespace Games.Api.Infrastructure.Events;

public class EventBridgePublisher
{
    private readonly IAmazonEventBridge _eventBridge;

    public EventBridgePublisher(IAmazonEventBridge eventBridge)
    {
        _eventBridge = eventBridge;
    }

    public async Task PublishAsync(IEvent @event)
    {
        var request = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    Source = "fcg.games",
                    DetailType = @event.Type,
                    Detail = JsonSerializer.Serialize(@event),
                    EventBusName = "default"
                }
            }
        };

        await _eventBridge.PutEventsAsync(request);
    }
}
