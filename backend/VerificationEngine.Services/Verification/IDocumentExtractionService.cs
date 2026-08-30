using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Verification;

public interface IDocumentExtractionService
{
    /// <summary>Reads labelled form fields off a scanned legal document.</summary>
    Task<ExtractedFields> ExtractAsync(string s3Key, CancellationToken cancellationToken = default);
}
