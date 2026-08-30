using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using VerificationEngine.Domain.Verification;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Verification;

/// <summary>
/// REAL. Amazon Rekognition <c>CompareFaces</c> stands in for a commercial biometric KYC
/// vendor here, and does the same job: it returns a genuine similarity score between the
/// claimant's selfie and the photo on their ID.
///
/// Rekognition reads both images straight out of S3, so no image bytes ever pass through
/// this Lambda — cheaper, faster, and one less place personal data can be logged.
/// </summary>
public sealed class RekognitionFaceVerificationService : IFaceVerificationService
{
    private readonly IAmazonRekognition _rekognition;
    private readonly EngineOptions _options;

    public RekognitionFaceVerificationService(IAmazonRekognition rekognition, EngineOptions options)
    {
        _rekognition = rekognition;
        _options = options;
    }

    public async Task<FaceComparisonResult> CompareAsync(
        string selfieS3Key, string idDocumentS3Key, CancellationToken cancellationToken = default)
    {
        var request = new CompareFacesRequest
        {
            // The ID document is the source of truth; the selfie is the claim being tested.
            SourceImage = S3Image(idDocumentS3Key),
            TargetImage = S3Image(selfieS3Key),

            // Ask Rekognition for anything above 70 so a near-miss comes back with a
            // usable score to show the claimant, then apply our own stricter threshold below.
            SimilarityThreshold = 70f
        };

        try
        {
            var response = await _rekognition.CompareFacesAsync(request, cancellationToken);

            var best = response.FaceMatches?
                .OrderByDescending(m => m.Similarity)
                .FirstOrDefault();

            if (best is null)
            {
                // Faces were found in both images but none of them matched.
                return new FaceComparisonResult(
                    IsMatch: false,
                    Similarity: 0,
                    Detail: "The face in your selfie does not match the photo on the ID document you uploaded. " +
                            "Please check you uploaded the right ID, then take a new photo.");
            }

            var similarity = best.Similarity ?? 0;

            return similarity >= FaceComparisonResult.MatchThreshold
                ? new FaceComparisonResult(true, similarity,
                    $"Your identity was confirmed with {similarity:F1}% certainty.")
                : new FaceComparisonResult(false, similarity,
                    $"We could only match your face with {similarity:F1}% certainty, and we require " +
                    $"{FaceComparisonResult.MatchThreshold:F0}%. Try again in brighter light, facing the camera directly.");
        }
        catch (InvalidParameterException)
        {
            // Rekognition throws this when it cannot detect a face at all — nearly always
            // a bad photo rather than a bad person, so it is worth its own message.
            return new FaceComparisonResult(
                IsMatch: false,
                Similarity: 0,
                Detail: "We could not find a clear face in one of the images. Make sure your whole face is visible, " +
                        "the photo is in focus, and the ID document is flat and well lit.");
        }
    }

    private Image S3Image(string key) => new()
    {
        S3Object = new S3Object { Bucket = _options.DocumentsBucket, Name = key }
    };
}
