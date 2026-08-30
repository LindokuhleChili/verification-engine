using Amazon.CDK;
using VerificationEngine.Infra;

var app = new App();

_ = new VerificationEngineStack(app, "VerificationEngineStack", new StackProps
{
    // No explicit Env set: deploys to whatever account/region `aws configure` points
    // at. Keeping it environment-agnostic is deliberate for a project with exactly one
    // real deployment target - see docs/DEPLOYING.md for how to pin it if that changes.
    Description = "Verification Engine - South African shareholder claims portfolio project. " +
                   "See docs/project-brief.md for scope and docs/design.md for the design system."
});

app.Synth();
