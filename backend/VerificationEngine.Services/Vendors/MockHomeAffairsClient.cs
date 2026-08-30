using VerificationEngine.Domain.Verification;

namespace VerificationEngine.Services.Vendors;

/// <summary>
/// SIMULATED. There is no public Home Affairs API — access requires accreditation as a
/// financial institution, so no portfolio project can call the real thing.
///
/// What is faked: the existence and contents of the population-register record.
/// What is NOT faked: the face comparison that consumes it. The reference photo is the
/// identity document the claimant actually uploaded, and Amazon Rekognition really does
/// compare it against their selfie.
///
/// Swapping in the real vendor means replacing this class and nothing else.
/// </summary>
public sealed class MockHomeAffairsClient : IHomeAffairsClient
{
    public Task<HomeAffairsRecord?> LookupAsync(string idNumber, CancellationToken cancellationToken = default)
    {
        if (!SouthAfricanIdNumber.IsValid(idNumber))
            return Task.FromResult<HomeAffairsRecord?>(null);

        var dateOfBirth = SouthAfricanIdNumber.DateOfBirth(idNumber)
                          ?? new DateOnly(1970, 1, 1);

        // Deterministic from the ID number so a demo replays identically every time.
        var record = new HomeAffairsRecord(
            IdNumber: idNumber,
            FullName: "Record held by Home Affairs",
            DateOfBirth: dateOfBirth,
            IsDeceased: false,
            ReferencePhotoS3Key: null);

        return Task.FromResult<HomeAffairsRecord?>(record);
    }
}
