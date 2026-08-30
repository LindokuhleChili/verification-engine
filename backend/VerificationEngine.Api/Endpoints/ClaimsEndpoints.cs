using Amazon.CognitoIdentityProvider;
using VerificationEngine.Api.Contracts;
using VerificationEngine.Api.Security;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Services.Events;
using VerificationEngine.Services.Persistence;
using VerificationEngine.Services.Storage;

namespace VerificationEngine.Api.Endpoints;

/// <summary>Create, list, read, and submit claims - the endpoints every claim type shares.</summary>
public static class ClaimsEndpoints
{
    public static void MapClaimsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/claims").RequireAuthorization();

        group.MapPost("/", CreateClaim);
        group.MapGet("/", ListMyClaims);
        group.MapGet("/{claimId}", GetClaim);
        group.MapPost("/{claimId}/submit", SubmitClaim);
        group.MapGet("/{claimId}/document", GetGeneratedDocumentUrl);
    }

    private static async Task<IResult> CreateClaim(
        HttpContext http, CreateClaimRequest request, IClaimRepository repository, IClaimEventPublisher events,
        IAmazonCognitoIdentityProvider cognito)
    {
        var claim = new Claim
        {
            ClaimId = Guid.NewGuid().ToString("N"),
            OwnerUserId = CurrentUser.Id(http),
            OwnerEmail = await CurrentUser.EmailAsync(http, cognito),
            ClaimType = request.ClaimType,
            Status = ClaimStatus.Draft,
            ShareholderFullName = request.ShareholderFullName,
            ShareholderIdNumber = request.ShareholderIdNumber,
            CompanyName = request.CompanyName,
            AmountCents = request.AmountCents,
            CertificateNumber = request.CertificateNumber
        };

        // Not required for step validation - our own SA ID number check is real logic
        // (see SouthAfricanIdNumber), independent of the mocked Home Affairs lookup.
        if (claim.ShareholderIdNumber is { } id && !Services.Vendors.SouthAfricanIdNumber.IsValid(id))
            return Results.BadRequest(new { error = "That does not look like a valid South African ID number." });

        await repository.SaveClaimAsync(claim);
        await events.PublishAsync(new ClaimEvent("ClaimCreated", new { claim.ClaimId, ClaimType = claim.ClaimType.ToString() }));

        return Results.Created($"/claims/{claim.ClaimId}", claim.ToSummary());
    }

    private static async Task<IResult> ListMyClaims(HttpContext http, IClaimRepository repository)
    {
        var claims = await repository.ListClaimsForUserAsync(CurrentUser.Id(http));
        return Results.Ok(claims.Select(c => c.ToSummary()));
    }

    private static async Task<IResult> GetClaim(HttpContext http, string claimId, IClaimRepository repository)
    {
        var aggregate = await repository.GetClaimAggregateAsync(claimId);
        if (aggregate is null) return Results.NotFound();
        var viewerId = CurrentUser.Id(http);
        if (!ClaimAccess.CanView(aggregate.Claim, aggregate.Steps, viewerId)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        return Results.Ok(aggregate.Claim.ToDetail(aggregate.Steps, aggregate.Documents, viewerId));
    }

    private static async Task<IResult> SubmitClaim(
        HttpContext http, string claimId, IClaimRepository repository, IClaimEventPublisher events)
    {
        var aggregate = await repository.GetClaimAggregateAsync(claimId);
        if (aggregate is null) return Results.NotFound();
        if (aggregate.Claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var byName = aggregate.Steps.ToDictionary(s => s.Name, s => s.Status);
        if (!ClaimWorkflow.CanSubmit(aggregate.Claim.ClaimType, byName))
            return Results.BadRequest(new { error = "Every verification step must pass before a claim can be submitted." });

        aggregate.Claim.Status = ClaimStatus.Pending;
        aggregate.Claim.SubmittedAt = DateTimeOffset.UtcNow;
        await repository.SaveClaimAsync(aggregate.Claim);

        // The deceased-estate Step Functions workflow (multi-party orchestration) picks
        // this event up off the shared bus; the other two claim types have nothing
        // listening yet at this event and complete synchronously when signed instead.
        //
        // ClaimType is explicitly .ToString()'d here: this goes through
        // EventBridgeClaimEventPublisher's own JsonSerializer.Serialize call, which
        // (unlike the ASP.NET Core pipeline - see Program.cs) has no JsonStringEnumConverter
        // configured, and the CDK stack's EventBridge rule pattern-matches this field as
        // a literal string ("DeceasedEstate") - see VerificationEngineStack.Workflow.cs.
        await events.PublishAsync(new ClaimEvent(
            "ClaimSubmitted", new { aggregate.Claim.ClaimId, ClaimType = aggregate.Claim.ClaimType.ToString() }));

        return Results.Ok(aggregate.Claim.ToSummary());
    }

    private static async Task<IResult> GetGeneratedDocumentUrl(
        HttpContext http, string claimId, IClaimRepository repository, IDocumentStore documentStore)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (claim.GeneratedDocumentKey is null) return Results.NotFound(new { error = "No document has been generated for this claim yet." });

        var url = await documentStore.CreateDownloadUrlAsync(claim.GeneratedDocumentKey, TimeSpan.FromMinutes(10));
        return Results.Ok(new { url });
    }
}
