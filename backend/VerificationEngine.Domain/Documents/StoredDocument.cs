namespace VerificationEngine.Domain.Documents;

/// <summary>
/// Metadata for one file the claimant uploaded. The bytes live in S3; only the key
/// is stored here, and nothing is ever served from a public URL — downloads go out
/// as short-lived presigned URLs.
/// </summary>
public sealed class StoredDocument
{
    public required string ClaimId { get; init; }
    public required string DocumentId { get; init; }
    public required DocumentType DocumentType { get; init; }
    public required string S3Key { get; init; }
    public required string ContentType { get; init; }

    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Which party uploaded it — matters for the two-party deceased-estate flow.</summary>
    public string? UploadedByUserId { get; set; }

    /// <summary>Set when a document is refused, with a reason specific enough for the claimant to act on.</summary>
    public string? RejectionReason { get; set; }
}
