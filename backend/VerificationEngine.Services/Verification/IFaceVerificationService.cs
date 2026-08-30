using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Verification;

public interface IFaceVerificationService
{
    /// <summary>Compares a live selfie against the face printed on an identity document.</summary>
    Task<FaceComparisonResult> CompareAsync(
        string selfieS3Key, string idDocumentS3Key, CancellationToken cancellationToken = default);
}
