using VerificationEngine.Services.Vendors;
using Xunit;

namespace VerificationEngine.Tests.Services;

/// <summary>
/// The mocks stand in for real vendors in demos, so their behaviour needs to be
/// predictable enough to script a walkthrough around - these tests pin that down.
/// </summary>
public sealed class MockVendorTests
{
    [Fact]
    public async Task MockStitchClient_StartLink_PointsAtOwnFrontendNotARealBank()
    {
        var client = new MockStitchClient("https://claims.example.com");

        var session = await client.StartLinkAsync("claim-123");

        Assert.StartsWith("https://claims.example.com/claims/claim-123/banking/consent", session.ConsentUrl);
        Assert.All(session.RequestedScopes, scope => Assert.DoesNotContain("write", scope, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MockStitchClient_CompleteLink_LabelsResultAsSimulated()
    {
        var client = new MockStitchClient("https://claims.example.com");
        var session = await client.StartLinkAsync("claim-123");

        var result = await client.CompleteLinkAsync(session.SessionId, "Jane Shareholder");

        Assert.True(result.IsVerified);
        Assert.Equal("Jane Shareholder", result.AccountHolderName);
        Assert.Equal(4, result.AccountLastFour.Length);
        Assert.StartsWith(MockNotice.Prefix, result.Detail);
    }

    [Fact]
    public async Task MockStitchClient_CompleteLink_RejectsASessionItDidNotIssue()
    {
        var client = new MockStitchClient("https://claims.example.com");

        var result = await client.CompleteLinkAsync("some-other-vendor-session-id", "Jane Shareholder");

        Assert.False(result.IsVerified);
    }

    [Fact]
    public async Task MockHomeAffairsClient_RejectsAnInvalidIdNumber()
    {
        var client = new MockHomeAffairsClient();

        var record = await client.LookupAsync("not-a-real-id-number");

        Assert.Null(record);
    }

    [Fact]
    public async Task MockHomeAffairsClient_ReturnsARecordForAWellFormedIdNumber()
    {
        var client = new MockHomeAffairsClient();

        var record = await client.LookupAsync("9001015000085");

        Assert.NotNull(record);
        Assert.Equal(new DateOnly(1990, 1, 1), record!.DateOfBirth);
    }

    [Fact]
    public async Task MockCourierClient_TrackingEventsAreLabelledSimulatedAndChronological()
    {
        var client = new MockCourierClient();

        var tracking = await client.TrackAsync("MOCK-TCG-ABC12345");

        Assert.All(tracking.Events, e => Assert.StartsWith(MockNotice.Prefix, e.Description));
        Assert.Equal(tracking.Events.OrderBy(e => e.OccurredAt), tracking.Events);
    }
}
