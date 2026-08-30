using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace VerificationEngine.Api.Security;

/// <summary>
/// Reads the identity API Gateway's Cognito JWT authorizer has already verified.
///
/// API Gateway validates the token's signature and expiry before the request ever
/// reaches Lambda, and Amazon.Lambda.AspNetCoreServer.Hosting turns the authorizer's
/// JWT claims into <c>HttpContext.User</c> automatically - so by the time an endpoint
/// runs, <see cref="Id"/> is just reading an already-trusted claim, not re-validating a
/// token.
/// </summary>
public static class CurrentUser
{
    /// <summary>The Cognito <c>sub</c> - the stable identifier to use as the claim owner, never the email.</summary>
    public static string Id(HttpContext context) =>
        context.User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Request reached an authenticated endpoint without a 'sub' claim.");

    /// <summary>
    /// Looks up the caller's email via Cognito's GetUser API, using their own access
    /// token. This is deliberately not a claim read: Cognito access tokens (the token
    /// type API Gateway's JWT authorizer validates, and the type AWS recommends for API
    /// authorization) carry only <c>sub</c>/<c>client_id</c>/<c>scope</c> - never profile
    /// attributes like email. Only ID tokens carry those, and ID tokens are meant for the
    /// client app to read locally, not for authorizing API calls - so the only correct
    /// way to learn the caller's email server-side is to ask Cognito with their token.
    /// </summary>
    public static async Task<string> EmailAsync(HttpContext context, IAmazonCognitoIdentityProvider cognito)
    {
        var accessToken = context.Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        var response = await cognito.GetUserAsync(new GetUserRequest { AccessToken = accessToken });

        return response.UserAttributes.FirstOrDefault(a => a.Name == "email")?.Value
            ?? throw new UnauthorizedAccessException("This Cognito user has no email attribute set.");
    }
}
