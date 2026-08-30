using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using DynamoAttribute = Amazon.CDK.AWS.DynamoDB.Attribute;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace VerificationEngine.Infra;

/// <summary>
/// Everything this project deploys, in one stack. A portfolio project this size gets
/// no benefit from splitting into nested stacks - it would only add cross-stack
/// reference plumbing to work around - so one stack keeps `cdk deploy` and `cdk diff`
/// legible as a single unit. Construction is split across partial-class files by
/// concern (data, auth, api, workflow, frontend) purely for readability.
/// </summary>
public sealed partial class VerificationEngineStack : Stack
{
    // Shared across the partial-class build methods below.
    private readonly Table _table;
    private readonly Bucket _documentsBucket;
    private readonly UserPool _userPool;
    private readonly UserPoolClient _userPoolClient;

    /// <summary>
    /// The React app's origin, used both for the S3/API CORS allow-list and for links
    /// embedded in emails (executor invites, etc). Passed as CDK context so it can
    /// differ between a first deploy (Amplify domain unknown yet) and later ones -
    /// see docs/DEPLOYING.md. Defaults to localhost so a first `cdk deploy` doesn't fail.
    /// </summary>
    private string FrontendBaseUrl => Node.TryGetContext("frontendBaseUrl") as string ?? "http://localhost:5173";

    private string SenderEmailAddress =>
        Node.TryGetContext("senderEmail") as string
        ?? throw new ArgumentException(
            "Context value 'senderEmail' is required - pass it with " +
            "-c senderEmail=you@example.com. It must be an address you can verify in SES " +
            "(SES stays in sandbox mode to remain free, so every recipient during testing " +
            "must also be verified individually - see docs/DEPLOYING.md).");

    public VerificationEngineStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        _table = BuildTable();
        _documentsBucket = BuildDocumentsBucket();
        (_userPool, _userPoolClient) = BuildAuth();

        var apiFunction = BuildApiFunction();
        var (apiUrl, httpApiId) = BuildHttpApi(apiFunction);

        var eventBus = BuildMessaging(out var notifierFunction, out var notificationQueue, out var notificationDlq);
        var stateMachine = BuildDeceasedEstateWorkflow(eventBus);

        BuildSesIdentity();
        BuildMonitoring(apiFunction, notifierFunction, notificationQueue, notificationDlq, stateMachine, httpApiId);

        EmitOutputs(apiUrl);
    }

    /// <summary>
    /// Single-table design - see VerificationEngine.Domain.Persistence.TableKeys for the
    /// full key schema this table stores. On-demand billing because claim volume in a
    /// demo is bursty and near-zero most of the time; provisioned capacity would either
    /// throttle during a demo burst or sit paying for idle capacity, and on-demand's
    /// free tier (25 WCU/RCU-equivalent workloads at this scale) covers this easily.
    /// </summary>
    private Table BuildTable()
    {
        var table = new Table(this, "ClaimsTable", new TableProps
        {
            TableName = "verification-engine-claims",
            PartitionKey = new DynamoAttribute { Name = "PK", Type = AttributeType.STRING },
            SortKey = new DynamoAttribute { Name = "SK", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            Encryption = TableEncryption.AWS_MANAGED,
            TimeToLiveAttribute = "ExpiresAtEpoch",
            // DESTROY (not RETAIN) is deliberate: this is a demo dataset with no real
            // claimants in it, and `cdk destroy` should not leave a paying orphan table.
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI1",
            PartitionKey = new DynamoAttribute { Name = "GSI1PK", Type = AttributeType.STRING },
            SortKey = new DynamoAttribute { Name = "GSI1SK", Type = AttributeType.STRING },
            ProjectionType = ProjectionType.ALL
        });

        return table;
    }

    /// <summary>
    /// Private document storage. All access is via short-lived presigned URLs (see
    /// S3DocumentStore) so the bucket itself never needs a public-read exception.
    /// </summary>
    private Bucket BuildDocumentsBucket() => new(this, "DocumentsBucket", new BucketProps
    {
        BucketName = $"verification-engine-documents-{Account}-{Region}",
        BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
        Encryption = BucketEncryption.S3_MANAGED,
        EnforceSSL = true,
        RemovalPolicy = RemovalPolicy.DESTROY,
        // Demo bucket: let `cdk destroy` actually remove it instead of leaving orphaned,
        // silently-billed objects behind after the project is torn down.
        AutoDeleteObjects = true,
        Cors =
        [
            new CorsRule
            {
                AllowedMethods = [HttpMethods.PUT, HttpMethods.GET],
                AllowedOrigins = [FrontendBaseUrl],
                AllowedHeaders = ["*"],
                MaxAge = 300
            }
        ]
    });

    /// <summary>
    /// Email/password sign-up with email verification. No hosted UI / OAuth domain -
    /// the React app talks to Cognito directly through the JS SDK, which is enough for
    /// a first-party frontend and avoids standing up a Cognito domain for no benefit.
    /// </summary>
    private (UserPool, UserPoolClient) BuildAuth()
    {
        var userPool = new UserPool(this, "UserPool", new UserPoolProps
        {
            UserPoolName = "verification-engine-users",
            SelfSignUpEnabled = true,
            SignInAliases = new SignInAliases { Email = true },
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            // Full name is collected in the app's own claim form (see Claim.ShareholderFullName)
            // rather than as a Cognito standard attribute - it isn't needed to authenticate.
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true, Mutable = true }
            },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 8,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigits = true,
                RequireSymbols = false
            },
            AccountRecovery = AccountRecovery.EMAIL_ONLY,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var userPoolClient = userPool.AddClient("WebClient", new UserPoolClientProps
        {
            UserPoolClientName = "verification-engine-web",
            AuthFlows = new AuthFlow { UserSrp = true, UserPassword = true },
            // Public single-page app - it cannot keep a client secret confidential.
            GenerateSecret = false,
            PreventUserExistenceErrors = true,
            AccessTokenValidity = Duration.Hours(1),
            RefreshTokenValidity = Duration.Days(30)
        });

        return (userPool, userPoolClient);
    }

    private void EmitOutputs(string apiUrl)
    {
        _ = new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = apiUrl, Description = "Base URL the frontend calls." });
        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = _userPool.UserPoolId });
        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = _userPoolClient.UserPoolClientId });
        _ = new CfnOutput(this, "DocumentsBucketName", new CfnOutputProps { Value = _documentsBucket.BucketName });
        _ = new CfnOutput(this, "TableName", new CfnOutputProps { Value = _table.TableName });
    }
}
