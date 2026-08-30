namespace VerificationEngine.Domain.Claims;

/// <summary>
/// The root record for one attempt to recover shares or dividends. Stored as the
/// <c>METADATA</c> item of its claim partition — see <see cref="Persistence.TableKeys"/>.
/// </summary>
public sealed class Claim
{
    public required string ClaimId { get; init; }

    /// <summary>Cognito <c>sub</c> of the claimant who created it. Never used as the notification address — emails change.</summary>
    public required string OwnerUserId { get; init; }

    /// <summary>
    /// The claimant's email at the time of creation, kept only so async workers (Step
    /// Functions tasks, EventBridge-triggered notifiers) can email them without a
    /// round trip to Cognito. A production system would re-fetch from Cognito instead,
    /// since this copy goes stale if the user changes their address later.
    /// </summary>
    public required string OwnerEmail { get; init; }

    public required ClaimType ClaimType { get; init; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

    /// <summary>SA ID number of the person the shares belong to. For a deceased estate this is the deceased, not the claimant.</summary>
    public string? ShareholderIdNumber { get; set; }
    public string? ShareholderFullName { get; set; }

    /// <summary>Free-text company name as the claimant knows it; no registry lookup exists to normalise it against.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Claim value in ZAR cents. Integer cents, never a float — this is money.</summary>
    public long? AmountCents { get; set; }

    /// <summary>Set only for <see cref="ClaimType.LostCertificate"/>.</summary>
    public string? CertificateNumber { get; set; }

    /// <summary>Master's Reference Number, OCR'd from the Letter of Executorship. Deceased-estate claims only.</summary>
    public string? MastersReferenceNumber { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>S3 key of the generated final document, once <see cref="ClaimStatus.Complete"/>.</summary>
    public string? GeneratedDocumentKey { get; set; }
}
