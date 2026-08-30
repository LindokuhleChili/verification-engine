namespace VerificationEngine.Services.Vendors;

/// <summary>
/// Physical collection of original documents. The Master's Office issues exactly one
/// original Letter of Executorship, so for a deceased estate a scan is not sufficient —
/// the paper has to travel.
/// </summary>
public interface ICourierClient
{
    Task<CourierBooking> BookCollectionAsync(string claimId, string collectionAddress, CancellationToken cancellationToken = default);
    Task<CourierTracking> TrackAsync(string waybillNumber, CancellationToken cancellationToken = default);
}

public sealed record CourierBooking(string WaybillNumber, DateOnly EstimatedCollectionDate);

public sealed record CourierTracking(string WaybillNumber, string Status, IReadOnlyList<CourierEvent> Events);

public sealed record CourierEvent(DateTimeOffset OccurredAt, string Description, string Location);
