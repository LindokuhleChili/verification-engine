using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;

namespace VerificationEngine.Infra;

public sealed partial class VerificationEngineStack
{
    /// <summary>
    /// One Lambda hosts the whole ASP.NET Core minimal API (see VerificationEngine.Api).
    /// A single function behind a proxy integration is simpler to reason about and
    /// operate than one Lambda per route, and at this traffic level there is no cold-
    /// start or scaling reason to split it - that trade only starts to matter at a
    /// scale this project will never reach.
    /// </summary>
    private Function BuildApiFunction()
    {
        var function = new Function(this, "ApiFunction", new FunctionProps
        {
            FunctionName = "verification-engine-api",
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.X86_64,
            MemorySize = 512,
            Tracing = Tracing.ACTIVE,
            // API Gateway itself times out an integration at 30s; leaving this just under
            // that means a slow Lambda produces our own JSON error, not API Gateway's.
            Timeout = Duration.Seconds(29),
            Handler = "VerificationEngine.Api",
            Code = DotnetLambdaAsset.FromProject("VerificationEngine.Api"),
            Environment = new Dictionary<string, string>
            {
                ["TABLE_NAME"] = _table.TableName,
                ["DOCUMENTS_BUCKET"] = _documentsBucket.BucketName,
                ["EVENT_BUS_NAME"] = "default",
                ["SENDER_EMAIL"] = SenderEmailAddress,
                ["FRONTEND_BASE_URL"] = FrontendBaseUrl
            }
        });

        _table.GrantReadWriteData(function);
        _documentsBucket.GrantReadWrite(function);
        GrantAiServiceAccess(function);
        GrantSesSendAccess(function);
        GrantEventBusPutEvents(function);

        return function;
    }

    /// <summary>
    /// Built from the raw AWS::ApiGatewayV2::* CloudFormation resources (CfnApi,
    /// CfnIntegration, CfnRoute, CfnAuthorizer, CfnStage) rather than the higher-level
    /// HttpApi/HttpLambdaIntegration/HttpJwtAuthorizer L2 constructs: those convenience
    /// classes shipped in a separate "alpha" package that AWS stopped publishing at
    /// v2.114 without ever graduating a replacement, so they cannot be paired with a
    /// current aws-cdk-lib version. The L1 resources are the stable, permanent surface
    /// underneath them and describe exactly the same infrastructure.
    /// </summary>
    private (string ApiUrl, string ApiId) BuildHttpApi(Function apiFunction)
    {
        var httpApi = new CfnApi(this, "HttpApi", new CfnApiProps
        {
            Name = "verification-engine-api",
            ProtocolType = "HTTP",
            CorsConfiguration = new CfnApi.CorsProperty
            {
                AllowOrigins = [FrontendBaseUrl],
                AllowMethods = ["*"],
                AllowHeaders = ["Authorization", "Content-Type"],
                MaxAge = 600
            }
        });

        // HTTP API Lambda proxy integrations always use POST as the integration method,
        // independent of which HTTP method(s) the route itself accepts.
        var integration = new CfnIntegration(this, "ApiIntegration", new CfnIntegrationProps
        {
            ApiId = httpApi.AttrApiId,
            IntegrationType = "AWS_PROXY",
            IntegrationUri = apiFunction.FunctionArn,
            IntegrationMethod = "POST",
            PayloadFormatVersion = "2.0"
        });
        var integrationTarget = $"integrations/{integration.AttrIntegrationId}";

        // Unauthenticated: lets a deploy/monitoring check confirm the Lambda is warm
        // without needing a Cognito token.
        _ = new CfnRoute(this, "HealthRoute", new CfnRouteProps
        {
            ApiId = httpApi.AttrApiId,
            RouteKey = "GET /health",
            Target = integrationTarget
        });

        var authorizer = new CfnAuthorizer(this, "CognitoAuthorizer", new CfnAuthorizerProps
        {
            ApiId = httpApi.AttrApiId,
            Name = "CognitoJwtAuthorizer",
            AuthorizerType = "JWT",
            IdentitySource = ["$request.header.Authorization"],
            JwtConfiguration = new CfnAuthorizer.JWTConfigurationProperty
            {
                Audience = [_userPoolClient.UserPoolClientId],
                Issuer = $"https://cognito-idp.{Region}.amazonaws.com/{_userPool.UserPoolId}"
            }
        });

        // Everything else requires a valid Cognito access token. API Gateway verifies
        // the JWT's signature and expiry before Lambda ever runs - see
        // VerificationEngine.Api.Security.CurrentUser for how the app reads the result.
        _ = new CfnRoute(this, "ProxyRoute", new CfnRouteProps
        {
            ApiId = httpApi.AttrApiId,
            RouteKey = "ANY /{proxy+}",
            Target = integrationTarget,
            AuthorizationType = "JWT",
            AuthorizerId = authorizer.AttrAuthorizerId
        });

        // A browser's CORS preflight is never authenticated by design, but "ANY" (used
        // above) matches OPTIONS too - without this more specific route, every preflight
        // request hit the JWT authorizer, got a 401, and the browser reported the whole
        // call as a generic "Failed to fetch" before the real request was ever sent. An
        // exact method match (OPTIONS) takes priority over "ANY" for the same path, so
        // this route intercepts preflight first; ASP.NET Core's own CORS middleware
        // (UseCors() in Program.cs) then answers it directly, unauthenticated.
        _ = new CfnRoute(this, "ProxyOptionsRoute", new CfnRouteProps
        {
            ApiId = httpApi.AttrApiId,
            RouteKey = "OPTIONS /{proxy+}",
            Target = integrationTarget
        });

        // $default is the one HTTP API stage that needs no stage name in the invoke URL,
        // and AutoDeploy means every route/integration change above ships without a
        // separate CfnDeployment resource to manage.
        _ = new CfnStage(this, "DefaultStage", new CfnStageProps
        {
            ApiId = httpApi.AttrApiId,
            StageName = "$default",
            AutoDeploy = true
        });

        apiFunction.AddPermission("ApiGatewayInvoke", new Permission
        {
            Principal = new ServicePrincipal("apigateway.amazonaws.com"),
            SourceArn = $"arn:aws:execute-api:{Region}:{Account}:{httpApi.AttrApiId}/*/*"
        });

        return (httpApi.AttrApiEndpoint, httpApi.AttrApiId);
    }

    /// <summary>Rekognition and Textract have no resource-level permissions to scope to - "*" is the only option AWS supports here.</summary>
    private static void GrantAiServiceAccess(Function function) => function.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
        Effect = Effect.ALLOW,
        Actions = ["rekognition:CompareFaces", "textract:AnalyzeDocument"],
        Resources = ["*"]
    }));

    private void GrantSesSendAccess(Function function) => function.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
        Effect = Effect.ALLOW,
        Actions = ["ses:SendEmail", "ses:SendRawEmail"],
        // Scoped to this account/region's SES identities rather than "*" - the one place
        // in this stack an AI-service-style wildcard genuinely isn't necessary.
        Resources = [$"arn:aws:ses:{Region}:{Account}:identity/*"]
    }));

    private void GrantEventBusPutEvents(Function function) => function.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
        Effect = Effect.ALLOW,
        Actions = ["events:PutEvents"],
        Resources = [$"arn:aws:events:{Region}:{Account}:event-bus/default"]
    }));
}
