using VerificationEngine.Domain.Documents;

namespace VerificationEngine.Services.Storage;

/// <summary>
/// Access to the private S3 bucket holding identity documents and generated forms.
/// The bucket blocks all public access; every read and write goes through a presigned
/// URL that expires in minutes, so a leaked link stops working almost immediately.
/// </summary>
public interface IDocumentStore
{
    /// <summary>
    /// Issues a short-lived URL the browser uploads straight to. Bytes never pass through
    /// Lambda — that keeps the request off the API Gateway 10 MB payload limit and off
    /// the Lambda duration bill.
    /// </summary>
    Task<PresignedUpload> CreateUploadUrlAsync(
        string claimId, DocumentType documentType, string contentType, CancellationToken cancellationToken = default);

    Task<string> CreateDownloadUrlAsync(string s3Key, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task PutBytesAsync(string s3Key, byte[] content, string contentType, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken = default);
}

public sealed record PresignedUpload(string DocumentId, string S3Key, string UploadUrl, DateTimeOffset ExpiresAt);
