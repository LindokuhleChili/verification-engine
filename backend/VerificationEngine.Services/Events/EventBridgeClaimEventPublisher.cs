using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Events;

/// <inheritdoc cref="IClaimEventPublisher"/>
public sealed class EventBridgeClaimEventPublisher : IClaimEventPublisher
{
    private readonly IAmazonEventBridge _eventBridge;
    private readonly EngineOptions _options;

    public EventBridgeClaimEventPublisher(IAmazonEventBridge eventBridge, EngineOptions options)
    {
        _eventBridge = eventBridge;
        _options = options;
    }

    public async Task PublishAsync(ClaimEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _eventBridge.PutEventsAsync(new PutEventsRequest
        {
            Entries =
            [
                new PutEventsRequestEntry
                {
                    EventBusName = _options.EventBusName,
                    Source = ClaimEvent.Source,
                    DetailType = domainEvent.DetailType,
                    Detail = JsonSerializer.Serialize(domainEvent.Detail)
                }
            ]
        }, cancellationToken);
    }
}
