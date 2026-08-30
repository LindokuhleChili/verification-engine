using Amazon.S3;
using Amazon.S3.Model;
using VerificationEngine.Domain.Documents;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Storage;

/// <inheritdoc cref="IDocumentStore"/>
public sealed class S3DocumentStore : IDocumentStore
{
    /// <summary>Long enough for a phone on a slow connection, short enough that a leaked URL is near-worthless.</summary>
    private static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(10);

    private readonly IAmazonS3 _s3;
    private readonly EngineOptions _options;

    public S3DocumentStore(IAmazonS3 s3, EngineOptions options)
    {
        _s3 = s3;
        _options = options;
    }

    public async Task<PresignedUpload> CreateUploadUrlAsync(
        string claimId, DocumentType documentType, string contentType, CancellationToken cancellationToken = default)
    {
        var documentId = Guid.NewGuid().ToString("N");

        // Claim id first so a claim's uploads form one prefix — that makes the
        // "delete everything for this claim" POPIA request a single prefix delete.
        var key = $"uploads/{claimId}/{documentType}/{documentId}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.DocumentsBucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(UploadUrlLifetime),
            ContentType = contentType
        };

        var url = await _s3.GetPreSignedURLAsync(request);

        return new PresignedUpload(documentId, key, url, DateTimeOffset.UtcNow.Add(UploadUrlLifetime));
    }

    public async Task<string> CreateDownloadUrlAsync(
        string s3Key, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.DocumentsBucket,
            Key = s3Key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        };

        return await _s3.GetPreSignedURLAsync(request);
    }

    public async Task PutBytesAsync(
        string s3Key, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.DocumentsBucket,
            Key = s3Key,
            InputStream = stream,
            ContentType = contentType
        }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_options.DocumentsBucket, s3Key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // The claimant asked for a presigned URL but never finished uploading.
            return false;
        }
    }
}
