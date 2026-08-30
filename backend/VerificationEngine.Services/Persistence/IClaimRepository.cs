using VerificationEngine.Domain.Claims;
using VerificationEngine.Domain.Documents;

namespace VerificationEngine.Services.Persistence;

/// <summary>
/// Every DynamoDB access pattern this application has. Keeping them behind one
/// interface means the single-table key design lives in exactly one implementation
/// and nothing above this layer has to know about PK/SK strings.
/// </summary>
public interface IClaimRepository
{
    Task SaveClaimAsync(Claim claim, CancellationToken cancellationToken = default);

    Task<Claim?> GetClaimAsync(string claimId, CancellationToken cancellationToken = default);

    /// <summary>One Query against GSI1: the claimant's claims, newest first.</summary>
    Task<IReadOnlyList<Claim>> ListClaimsForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>One Query against the claim partition, returning the claim with its steps and documents.</summary>
    Task<ClaimAggregate?> GetClaimAggregateAsync(string claimId, CancellationToken cancellationToken = default);

    Task SaveStepAsync(VerificationStep step, CancellationToken cancellationToken = default);

    Task SaveDocumentAsync(StoredDocument document, CancellationToken cancellationToken = default);

    Task SaveInviteAsync(ExecutorInvite invite, CancellationToken cancellationToken = default);

    Task<ExecutorInvite?> GetInviteAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>A claim and everything in its partition, loaded together.</summary>
public sealed record ClaimAggregate(
    Claim Claim,
    IReadOnlyList<VerificationStep> Steps,
    IReadOnlyList<StoredDocument> Documents);

/// <summary>
/// A single-use, expiring invitation for an executor to join a deceased-estate claim.
/// The token is the partition key, so redeeming an invite is a point read and an
/// unknown token cannot be enumerated.
/// </summary>
public sealed class ExecutorInvite
{
    public required string Token { get; init; }
    public required string ClaimId { get; init; }
    public required string InvitedEmail { get; init; }
    public string? AcceptedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.UtcNow.AddDays(14);

    public bool IsRedeemable => AcceptedByUserId is null && DateTimeOffset.UtcNow < ExpiresAt;
}
