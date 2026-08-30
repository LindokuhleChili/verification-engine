using Amazon.Lambda.AspNetCoreServer.Hosting;
using QuestPDF.Infrastructure;
using VerificationEngine.Api.Endpoints;
using VerificationEngine.Services;

// QuestPDF's Community license is free for this use (individual, non-commercial
// portfolio project) but must be selected explicitly or document generation throws.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Wraps this ASP.NET Core app in a Lambda handler when running under Lambda, and does
// nothing when running locally with `dotnet run` - the same binary serves both.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddVerificationEngineServices();

// API Gateway's HTTP API JWT authorizer has already verified the Cognito access token
// before the request reaches Lambda; this only needs to know how to read the claims
// Amazon.Lambda.AspNetCoreServer.Hosting has already placed on HttpContext.User.
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

// No app.UseAuthentication(): API Gateway's JWT authorizer is the authentication step.
// It rejects an invalid/missing token before Lambda ever runs, and for a valid one,
// Amazon.Lambda.AspNetCoreServer.Hosting arrives having already built an authenticated
// ClaimsPrincipal from the authorizer's JWT claims - this only needs to enforce policies.
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestampUtc = DateTimeOffset.UtcNow }));

app.MapClaimsEndpoints();
app.MapVerificationEndpoints();
app.MapExecutorEndpoints();

app.Run();
