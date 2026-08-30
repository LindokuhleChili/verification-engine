namespace VerificationEngine.Services.Events;

/// <summary>
/// Publishes domain events onto the shared EventBridge bus so side effects (emails,
/// the deceased-estate Step Functions workflow) stay decoupled from the API handlers
/// that trigger them - a handler finishes as soon as it has written its own state.
/// </summary>
public interface IClaimEventPublisher
{
    Task PublishAsync(ClaimEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <param name="DetailType">Matches an EventBridge rule's <c>detail-type</c> filter, e.g. "ClaimSubmitted".</param>
/// <param name="Detail">Serialized as the event's JSON detail payload.</param>
public sealed record ClaimEvent(string DetailType, object Detail)
{
    public const string Source = "verification-engine.claims";
}
