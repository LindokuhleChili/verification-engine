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
    /// Amplify Hosting is deliberately NOT defined here - it's the one piece of this
    /// project created by hand in the console rather than by CDK, for a reason that
    /// isn't a shortcut: an early version of this stack DID create the Amplify App and
    /// Branch as CfnApp/CfnBranch resources, but a CfnApp with no repository configured
    /// at creation time cannot have GitHub attached to it afterwards through any
    /// interactive flow the Amplify console currently offers - only Amplify's own
    /// "New app -> Host a web app -> GitHub" wizard can create an app that is wired to
    /// GitHub's App-based integration (the non-deprecated one; the older personal-
    /// access-token flow that CDK's CfnApp resource still technically supports is the
    /// flow GitHub has deprecated for new connections). That wizard always creates a
    /// fresh App resource, so there is no CloudFormation-expressible version of "attach
    /// GitHub to this existing app" - the CDK-created placeholder had to be deleted and
    /// recreated through the console instead.
    ///
    /// Everything the console wizard needed (build spec, the app root inside this
    /// monorepo, and the four VITE_* environment variables pointing at Cognito/API
    /// Gateway) mirrors exactly what this file used to generate as CDK - see git
    /// history on this file for the code, and docs/DEPLOYING.md for the console steps.
    /// </summary>
}
