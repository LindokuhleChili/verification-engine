using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Api.Security;

/// <summary>
/// Who may view a claim: its owner, or - for a Deceased Estate claim - the executor
/// once they have verified and become a party on it. Everywhere else in the API,
/// "owner only" is enough; this is the one place a second party legitimately exists.
/// </summary>
public static class ClaimAccess
{
    public static bool CanView(Claim claim, IReadOnlyList<VerificationStep> steps, string userId) =>
        claim.OwnerUserId == userId || steps.Any(s => s.PartyUserId == userId);
}
