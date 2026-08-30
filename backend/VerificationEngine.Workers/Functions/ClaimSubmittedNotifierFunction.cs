using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using VerificationEngine.Services;
using VerificationEngine.Services.Notifications;
using VerificationEngine.Services.Persistence;

namespace VerificationEngine.Workers.Functions;

/// <summary>
/// Consumes "ClaimSubmitted" events off the SQS queue that subscribes to the
/// EventBridge bus, and emails the claimant a confirmation. Deliberately decoupled
/// from the API handler that publishes the event: the claimant's HTTP request to
/// submit a claim returns as soon as the event is published, it does not wait on SES.
///
/// SQS sits between EventBridge and this function so a transient SES failure retries
/// automatically and, after enough failures, lands on a dead-letter queue instead of
/// silently dropping the notification - the durability an EventBridge target alone
/// does not give you for free.
/// </summary>
public sealed class ClaimSubmittedNotifierFunction
{
    private readonly IClaimRepository _repository;
    private readonly INotificationService _notifications;

    public ClaimSubmittedNotifierFunction()
    {
        var services = new ServiceCollection().AddVerificationEngineServices().BuildServiceProvider();
        _repository = services.GetRequiredService<IClaimRepository>();
        _notifications = services.GetRequiredService<INotificationService>();
    }

    private sealed record EventBridgeEnvelope(JsonElement Detail);
    private sealed record ClaimSubmittedDetail(string ClaimId, string ClaimType);

    // AWS's own envelope fields ("detail", "detail-type", "source", ...) are always
    // lowercase; case-insensitive matching lets the envelope record still use the
    // PascalCase this codebase uses everywhere else.
    private static readonly JsonSerializerOptions EnvelopeOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var record in sqsEvent.Records)
        {
            var envelope = JsonSerializer.Deserialize<EventBridgeEnvelope>(record.Body, EnvelopeOptions)
                ?? throw new InvalidOperationException("Could not parse the EventBridge envelope from the SQS message body.");

            var detail = envelope.Detail.Deserialize<ClaimSubmittedDetail>()
                ?? throw new InvalidOperationException("EventBridge event detail did not match the expected ClaimSubmitted shape.");

            var claim = await _repository.GetClaimAsync(detail.ClaimId);
            if (claim is null)
            {
                context.Logger.LogWarning($"ClaimSubmitted event referenced unknown claim {detail.ClaimId}; skipping.");
                continue;
            }

            await _notifications.SendClaimStatusChangedAsync(
                claim.OwnerEmail,
                claim.ClaimId,
                "Pending",
                "We've received your claim and verification is underway. We'll email you again the moment there's an update.");
        }
    }
}
