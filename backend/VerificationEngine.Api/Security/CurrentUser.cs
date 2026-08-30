namespace VerificationEngine.Api.Security;

/// <summary>
/// Reads the identity API Gateway's Cognito JWT authorizer has already verified.
///
/// API Gateway validates the token's signature and expiry before the request ever
/// reaches Lambda, and Amazon.Lambda.AspNetCoreServer.Hosting turns the authorizer's
/// JWT claims into <c>HttpContext.User</c> automatically - so by the time an endpoint
/// runs, this is just reading already-trusted claims, not re-validating a token.
/// </summary>
public static class CurrentUser
{
    /// <summary>The Cognito <c>sub</c> - the stable identifier to use as the claim owner, never the email.</summary>
    public static string Id(HttpContext context) =>
        context.User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Request reached an authenticated endpoint without a 'sub' claim.");

    public static string Email(HttpContext context) =>
        context.User.FindFirst("email")?.Value
        ?? context.User.FindFirst("cognito:username")?.Value
        ?? throw new UnauthorizedAccessException("Request reached an authenticated endpoint without an email claim.");
}
