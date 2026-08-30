using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Vendors;

/// <summary>
/// Open-banking account verification. The claimant authorises read-only access at
/// their own bank; we receive confirmation that the account exists and whose name is
/// on it, and never see their banking credentials.
/// </summary>
public interface IStitchClient
{
    /// <summary>Begins a consent session and returns the URL to redirect the claimant to.</summary>
    Task<BankLinkSession> StartLinkAsync(string claimId, CancellationToken cancellationToken = default);

    /// <summary>Exchanges the consent token for the verified account details.</summary>
    Task<BankAccountVerificationResult> CompleteLinkAsync(
        string sessionId, string expectedAccountHolderName, CancellationToken cancellationToken = default);
}

public sealed record BankLinkSession(string SessionId, string ConsentUrl, IReadOnlyList<string> RequestedScopes);
