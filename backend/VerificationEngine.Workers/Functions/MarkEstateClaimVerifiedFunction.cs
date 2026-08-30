using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Services;
using VerificationEngine.Services.Notifications;
using VerificationEngine.Services.Persistence;

namespace VerificationEngine.Workers.Functions;

/// <summary>
/// The Deceased Estate state machine's success path: both parties and the courier
/// delivery passed, so the claim moves to Verified and both the beneficiary and the
/// executor are emailed. The joint CM42 signature (and PDF generation) still happens
/// through the same <c>/verification/signature</c> endpoint the other claim types use -
/// this only flips the status the frontend gates that step on.
/// </summary>
public sealed class MarkEstateClaimVerifiedFunction
{
    private readonly IClaimRepository _repository;
    private readonly INotificationService _notifications;

    public MarkEstateClaimVerifiedFunction()
    {
        var services = new ServiceCollection().AddVerificationEngineServices().BuildServiceProvider();
        _repository = services.GetRequiredService<IClaimRepository>();
        _notifications = services.GetRequiredService<INotificationService>();
    }

    public sealed record Input(string ClaimId);

    public async Task FunctionHandler(Input input, ILambdaContext context)
    {
        var claim = await _repository.GetClaimAsync(input.ClaimId)
            ?? throw new InvalidOperationException($"Claim {input.ClaimId} was not found.");

        claim.Status = ClaimStatus.Verified;
        await _repository.SaveClaimAsync(claim);

        context.Logger.LogInformation($"Claim {input.ClaimId} verified. Notifying claimant.");

        // Only the claimant's own address is stored on the claim (see Claim.OwnerEmail);
        // the executor already received their own confirmation when they accepted the
        // invite, and their email lives only on the ExecutorInvite record, not here.
        await _notifications.SendClaimStatusChangedAsync(
            claim.OwnerEmail,
            claim.ClaimId,
            ClaimStatus.Verified.ToString(),
            "Both parties and the courier delivery have been confirmed. You may now sign the CM42 transfer form.");
    }
}
