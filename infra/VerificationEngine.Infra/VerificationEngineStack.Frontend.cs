using Amazon.CDK;
using Amazon.CDK.AWS.Amplify;
using Amazon.CDK.AWS.SES;

namespace VerificationEngine.Infra;

public sealed partial class VerificationEngineStack
{
    /// <summary>
    /// Registers the sender address with SES as code, but cannot finish verifying it -
    /// AWS emails a confirmation link only the account holder can click. SES also stays
    /// in sandbox mode deliberately (never requested a production access increase):
    /// production access requires an AWS support case and is meant for real send
    /// volume, neither of which fits a demo project, and staying sandboxed costs
    /// nothing while proving the same SendEmail integration works.
    /// </summary>
    private void BuildSesIdentity() =>
        new EmailIdentity(this, "SenderIdentity", new EmailIdentityProps
        {
            Identity = Identity.Email(SenderEmailAddress)
        });

    /// <summary>
    /// Defines the Amplify Hosting app and its build spec as code, but the GitHub
    /// connection itself is finished by hand in the console: Amplify's current,
    /// non-deprecated way to connect a repo is its GitHub App integration, which
    /// requires an interactive OAuth authorization - there is no way to script that
    /// step, and the older personal-access-token flow CDK's L1/L2 constructs still
    /// technically support is the flow GitHub itself has deprecated for new
    /// connections. See docs/DEPLOYING.md for the exact console step.
    /// </summary>
    private void BuildFrontendHosting(string apiUrl)
    {
        var amplifyApp = new CfnApp(this, "FrontendApp", new CfnAppProps
        {
            Name = "verification-engine",
            Platform = "WEB",
            // Typed as `object` by the generated binding (it accepts a token as well as
            // a literal list), so this needs an explicit array rather than a collection
            // expression, which can't infer an element type from an `object` target.
            EnvironmentVariables = new CfnApp.EnvironmentVariableProperty[]
            {
                new() { Name = "VITE_API_BASE_URL", Value = apiUrl },
                new() { Name = "VITE_COGNITO_USER_POOL_ID", Value = _userPool.UserPoolId },
                new() { Name = "VITE_COGNITO_CLIENT_ID", Value = _userPoolClient.UserPoolClientId },
                new() { Name = "VITE_AWS_REGION", Value = Region }
            },
            BuildSpec = """
                version: 1
                applications:
                  - appRoot: frontend
                    frontend:
                      phases:
                        preBuild:
                          commands:
                            - npm ci
                        build:
                          commands:
                            - npm run build
                      artifacts:
                        baseDirectory: dist
                        files:
                          - '**/*'
                      cache:
                        paths:
                          - node_modules/**/*
                """,
            // A SPA using client-side routing needs every path to resolve to index.html
            // and let React Router take over - without this, refreshing on any route
            // other than "/" returns Amplify's 404 page instead of the app.
            CustomRules = new CfnApp.CustomRuleProperty[]
            {
                new() { Source = "/<*>", Target = "/index.html", Status = "200" }
            }
        });

        _ = new CfnBranch(this, "MainBranch", new CfnBranchProps
        {
            AppId = amplifyApp.AttrAppId,
            BranchName = "main",
            EnableAutoBuild = true
        });

        _ = new CfnOutput(this, "AmplifyAppId", new CfnOutputProps
        {
            Value = amplifyApp.AttrAppId,
            Description = "Connect this app's 'main' branch to the GitHub repo in the Amplify console - see docs/DEPLOYING.md."
        });
    }
}
