using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Vendors;

/// <summary>
/// SIMULATED. Real open banking in South Africa requires a commercial contract with a
/// licensed provider (Stitch, Ozow, Mono) plus live bank accounts to test against —
/// neither is obtainable for a portfolio build, and both cost money.
///
/// What is faked: the bank connection and the account details that come back.
/// What is NOT faked: the consent flow's shape. The claimant is still shown exactly
/// which scopes are requested and must approve them, the session is still single-use,
/// and the full account number is still discarded — only the last four digits are kept,
/// because nothing downstream needs more than that.
/// </summary>
public sealed class MockStitchClient : IStitchClient
{
    /// <summary>Read-only scopes only. A dividend claim never needs permission to move money.</summary>
    private static readonly string[] Scopes =
    [
        "accountholders",
        "accounts",
        "accounts.verify"
    ];

    private readonly string _frontendBaseUrl;

    public MockStitchClient(string frontendBaseUrl) => _frontendBaseUrl = frontendBaseUrl.TrimEnd('/');

    public Task<BankLinkSession> StartLinkAsync(string claimId, CancellationToken cancellationToken = default)
    {
        var sessionId = $"mock-sess-{Guid.NewGuid():N}";

        // Points back at our own simulated consent screen rather than a bank's domain,
        // so nobody can mistake the demo for a real banking redirect.
        var consentUrl = $"{_frontendBaseUrl}/claims/{claimId}/banking/consent?session={sessionId}";

        return Task.FromResult(new BankLinkSession(sessionId, consentUrl, Scopes));
    }

    public Task<BankAccountVerificationResult> CompleteLinkAsync(
        string sessionId, string expectedAccountHolderName, CancellationToken cancellationToken = default)
    {
        if (!sessionId.StartsWith("mock-sess-", StringComparison.Ordinal))
        {
            return Task.FromResult(new BankAccountVerificationResult(
                IsVerified: false,
                BankName: string.Empty,
                AccountLastFour: string.Empty,
                AccountHolderName: string.Empty,
                Detail: MockNotice.Wrap("The banking consent session has expired. Please start the bank link again.")));
        }

        // Deterministic digits derived from the session so a demo shows stable values.
        var lastFour = Math.Abs(sessionId.GetHashCode() % 10000).ToString("D4");

        return Task.FromResult(new BankAccountVerificationResult(
            IsVerified: true,
            BankName: "Standard Bank of South Africa",
            AccountLastFour: lastFour,
            AccountHolderName: expectedAccountHolderName,
            Detail: MockNotice.Wrap("Account confirmed as active and in the claimant's name. No real bank was contacted.")));
    }
}
