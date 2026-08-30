namespace VerificationEngine.Domain.Claims;

/// <summary>
/// One checkpoint's outcome, stored as its own item so a step can be updated
/// without rewriting (and racing on) the whole claim.
/// </summary>
public sealed class VerificationStep
{
    public required string ClaimId { get; init; }
    public required VerificationStepName Name { get; init; }
    public VerificationStepStatus Status { get; set; } = VerificationStepStatus.NotStarted;

    /// <summary>Shown to the claimant verbatim, so it must be specific and non-technical.</summary>
    public string? Detail { get; set; }

    /// <summary>Rekognition similarity score, when this is the biometric step. 0–100.</summary>
    public double? ConfidenceScore { get; set; }

    /// <summary>Which party this step belongs to, for the two-party deceased-estate flow.</summary>
    public string? PartyUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
