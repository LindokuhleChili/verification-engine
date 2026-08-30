using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.Textract;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.Extensions.DependencyInjection;
using VerificationEngine.Services.Configuration;
using VerificationEngine.Services.Documents;
using VerificationEngine.Services.Events;
using VerificationEngine.Services.Notifications;
using VerificationEngine.Services.Persistence;
using VerificationEngine.Services.Storage;
using VerificationEngine.Services.Verification;
using VerificationEngine.Services.Vendors;

namespace VerificationEngine.Services;

/// <summary>
/// One registration list shared by both Lambda compute projects (the HTTP API and the
/// async workers) so the two never drift into registering different implementations.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVerificationEngineServices(this IServiceCollection services)
    {
        // Every AWS Lambda function in this project has X-Ray active tracing turned on
        // (see the Tracing = Tracing.ACTIVE CDK settings) - that alone captures each
        // invocation as a trace segment, but this line additionally instruments the AWS
        // SDK clients constructed below so calls to DynamoDB, S3, Rekognition, Textract,
        // SES, and EventBridge each show up as their own subsegment with their own
        // duration, rather than the whole request looking like one opaque block of time.
        AWSSDKHandler.RegisterXRayForAllServices();

        var options = EngineOptions.FromEnvironment();
        services.AddSingleton(options);

        // AWS SDK clients: singletons, since they are thread-safe and hold connection pools.
        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
        services.AddSingleton<IAmazonRekognition>(_ => new AmazonRekognitionClient());
        services.AddSingleton<IAmazonTextract>(_ => new AmazonTextractClient());
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => new AmazonSimpleEmailServiceV2Client());
        services.AddSingleton<IAmazonEventBridge>(_ => new AmazonEventBridgeClient());

        // Real AWS-backed services.
        services.AddSingleton<IClaimRepository, DynamoDbClaimRepository>();
        services.AddSingleton<IDocumentStore, S3DocumentStore>();
        services.AddSingleton<IFaceVerificationService, RekognitionFaceVerificationService>();
        services.AddSingleton<IDocumentExtractionService, TextractDocumentExtractionService>();
        services.AddSingleton<INotificationService, SesNotificationService>();
        services.AddSingleton<IClaimEventPublisher, EventBridgeClaimEventPublisher>();

        // Document generation.
        services.AddSingleton<IClaimDocumentGenerator, SarsDividendFormGenerator>();
        services.AddSingleton<IClaimDocumentGenerator, Cm42TransferFormGenerator>();
        services.AddSingleton<IClaimDocumentGenerator, IndemnityAffidavitGenerator>();
        services.AddSingleton<ClaimDocumentGeneratorFactory>();

        // Mocked third-party vendors - see each Mock* class for exactly what is faked and why.
        services.AddSingleton<IHomeAffairsClient, MockHomeAffairsClient>();
        services.AddSingleton<IStitchClient>(_ => new MockStitchClient(options.FrontendBaseUrl));
        services.AddSingleton<ICourierClient, MockCourierClient>();

        return services;
    }
}
