using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Vendors;

/// <summary>
/// Lookup of a citizen's record against the Department of Home Affairs population
/// register — in production this is reached through an accredited aggregator such as
/// Smile ID, never directly.
/// </summary>
public interface IHomeAffairsClient
{
    Task<HomeAffairsRecord?> LookupAsync(string idNumber, CancellationToken cancellationToken = default);
}

/// <param name="ReferencePhotoS3Key">
/// Where the authoritative face photo lives. In production this comes back from the
/// aggregator; here it is the identity document the claimant uploaded, which is why
/// the face comparison itself is real even though the record lookup is not.
/// </param>
public sealed record HomeAffairsRecord(
    string IdNumber,
    string FullName,
    DateOnly DateOfBirth,
    bool IsDeceased,
    string? ReferencePhotoS3Key);
