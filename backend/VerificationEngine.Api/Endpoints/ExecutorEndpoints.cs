using VerificationEngine.Api.Contracts;
using VerificationEngine.Api.Security;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Services.Notifications;
using VerificationEngine.Services.Persistence;

namespace VerificationEngine.Api.Endpoints;

/// <summary>
/// Inviting and accepting the second party on a Deceased Estate claim. The executor is
/// a separate Cognito user who did not create the claim, so they reach it only through
/// a single-use, expiring token emailed via SES.
/// </summary>
public static class ExecutorEndpoints
{
    public static void MapExecutorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/claims/{claimId}/executor/invite", InviteExecutor).RequireAuthorization();
        app.MapPost("/executor/accept", AcceptInvite).RequireAuthorization();
    }

    private static async Task<IResult> InviteExecutor(
        HttpContext http, string claimId, InviteExecutorRequest request,
        IClaimRepository repository, INotificationService notifications)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (claim.ClaimType != ClaimType.DeceasedEstate)
            return Results.BadRequest(new { error = "Only a Deceased Estate claim has an executor to invite." });

        var invite = new ExecutorInvite
        {
            Token = Guid.NewGuid().ToString("N"),
            ClaimId = claimId,
            InvitedEmail = request.ExecutorEmail
        };
        await repository.SaveInviteAsync(invite);

        var options = Services.Configuration.EngineOptions.FromEnvironment();
        var link = $"{options.FrontendBaseUrl.TrimEnd('/')}/executor/accept?token={invite.Token}";
        await notifications.SendExecutorInviteAsync(request.ExecutorEmail, claimId, link);

        return Results.Ok(new { invited = true });
    }

    private static async Task<IResult> AcceptInvite(
        HttpContext http, AcceptInviteRequest request, IClaimRepository repository)
    {
        var invite = await repository.GetInviteAsync(request.Token);
        if (invite is null) return Results.NotFound(new { error = "This invitation link is not valid." });
        if (!invite.IsRedeemable) return Results.BadRequest(new { error = "This invitation has already been used or has expired." });

        invite.AcceptedByUserId = CurrentUser.Id(http);
        await repository.SaveInviteAsync(invite);

        // Recording the party against the ExecutorIdentity step (NotStarted, no score
        // yet) is what makes ClaimAccess.CanView start letting this user see the claim -
        // the actual biometric pass happens on their first face-compare call.
        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = invite.ClaimId,
            Name = VerificationStepName.ExecutorIdentity,
            Status = VerificationStepStatus.NotStarted,
            PartyUserId = CurrentUser.Id(http),
            Detail = "Executor accepted the invitation and must now verify their identity."
        });

        return Results.Ok(new { claimId = invite.ClaimId });
    }
}
