namespace VerificationEngine.Services.Vendors;

/// <summary>
/// SIMULATED. The Courier Guy's API is commercial and every call books a real,
/// billable collection — not something to point a demo at.
///
/// What is faked: the booking and the tracking events.
/// What is NOT faked: the waybill is stored against the claim and the tracking card
/// renders from whatever the client returns, so pointing this at the real API is a
/// one-class change.
/// </summary>
public sealed class MockCourierClient : ICourierClient
{
    private readonly TimeProvider _time;

    public MockCourierClient(TimeProvider? time = null) => _time = time ?? TimeProvider.System;

    public Task<CourierBooking> BookCollectionAsync(
        string claimId, string collectionAddress, CancellationToken cancellationToken = default)
    {
        var waybill = $"MOCK-TCG-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var collectionDate = DateOnly.FromDateTime(_time.GetUtcNow().AddDays(1).UtcDateTime);

        return Task.FromResult(new CourierBooking(waybill, collectionDate));
    }

    public Task<CourierTracking> TrackAsync(string waybillNumber, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow();

        // A plausible two-day journey, generated relative to "now" so the card always
        // looks live rather than showing stale hard-coded dates.
        var events = new List<CourierEvent>
        {
            new(now.AddHours(-30), MockNotice.Wrap("Collection booked"), "Johannesburg"),
            new(now.AddHours(-22), MockNotice.Wrap("Parcel collected from executor"), "Sandton"),
            new(now.AddHours(-6),  MockNotice.Wrap("In transit to processing hub"), "Isando")
        };

        return Task.FromResult(new CourierTracking(waybillNumber, "In transit", events));
    }
}
