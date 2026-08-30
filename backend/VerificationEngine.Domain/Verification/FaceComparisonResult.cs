namespace VerificationEngine.Domain.Verification;

/// <summary>Outcome of comparing a live selfie against the face on an identity document.</summary>
public sealed record FaceComparisonResult(bool IsMatch, double Similarity, string Detail)
{
    /// <summary>
    /// Similarity below which we refuse the claim. 90 is deliberately stricter than
    /// Rekognition's 80 default: a false accept here means handing someone else's
    /// money to the wrong person, which is far worse than asking for a second selfie.
    /// </summary>
    public const double MatchThreshold = 90d;
}
