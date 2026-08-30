namespace VerificationEngine.Services.Configuration;

/// <summary>
/// Resource names injected by CDK as Lambda environment variables. Nothing here is a
/// secret, so there is no Secrets Manager dependency — table and bucket names are not
/// credentials, and IAM decides who may touch them.
/// </summary>
public sealed class EngineOptions
{
    public required string TableName { get; init; }
    public required string DocumentsBucket { get; init; }
    public required string EventBusName { get; init; }

    /// <summary>Verified SES identity emails are sent from. SES stays in sandbox, so recipients must also be verified.</summary>
    public required string SenderEmailAddress { get; init; }

    /// <summary>Public origin of the React app, used to build links inside emails.</summary>
    public required string FrontendBaseUrl { get; init; }

    public static EngineOptions FromEnvironment() => new()
    {
        TableName = Require("TABLE_NAME"),
        DocumentsBucket = Require("DOCUMENTS_BUCKET"),
        EventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME") ?? "default",
        SenderEmailAddress = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? "no-reply@example.com",
        FrontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5173"
    };

    /// <summary>Fail loudly at cold start rather than with a null reference on the first request.</summary>
    private static string Require(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required environment variable '{name}' is not set. Check the CDK stack wiring.");
}
