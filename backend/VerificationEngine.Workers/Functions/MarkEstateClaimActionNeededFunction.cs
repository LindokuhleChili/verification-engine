using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Services;
using VerificationEngine.Services.Notifications;
using VerificationEngine.Services.Persistence;

namespace VerificationEngine.Workers.Functions;

/// <summary>
/// The Deceased Estate state machine's timeout path: after the Wait/Choice loop's
/// retry budget is exhausted (see infra/StateMachines/deceased-estate.asl.json),
/// the claim needs a human, not another poll. Flags it and emails the claimant what
/// is still outstanding rather than leaving it silently pending forever.
/// </summary>
public sealed class MarkEstateClaimActionNeededFunction
{
    private readonly IClaimRepository _repository;
    private readonly INotificationService _notifications;

    public MarkEstateClaimActionNeededFunction()
    {
        var services = new ServiceCollection().AddVerificationEngineServices().BuildServiceProvider();
        _repository = services.GetRequiredService<IClaimRepository>();
        _notifications = services.GetRequiredService<INotificationService>();
    }

    public sealed record Input(string ClaimId, bool BeneficiaryVerified, bool ExecutorVerified, bool CourierDelivered);

    public async Task FunctionHandler(Input input, ILambdaContext context)
    {
        var claim = await _repository.GetClaimAsync(input.ClaimId)
            ?? throw new InvalidOperationException($"Claim {input.ClaimId} was not found.");

        claim.Status = ClaimStatus.ActionNeeded;
        await _repository.SaveClaimAsync(claim);

        var outstanding = new List<string>();
        if (!input.BeneficiaryVerified) outstanding.Add("your own identity verification");
        if (!input.ExecutorVerified) outstanding.Add("the executor's identity verification");
        if (!input.CourierDelivered) outstanding.Add("delivery of the original Letter of Executorship");

        context.Logger.LogWarning($"Claim {input.ClaimId} timed out waiting on: {string.Join(", ", outstanding)}");

        await _notifications.SendActionNeededAsync(
            claim.OwnerEmail,
            claim.ClaimId,
            $"We're still waiting on: {string.Join(", ", outstanding)}. " +
            "Please complete the outstanding step(s), or contact support if you believe this is an error.");
    }
}
