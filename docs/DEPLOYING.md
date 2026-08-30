# Deploying Verification Engine

This is the working runbook used to actually deploy this project. (A separate,
beginner-friendly walkthrough of the *finished* system — written last, per
`docs/project-brief.md` section 9 — will replace or supplement this once the
project owner confirms they're happy with it.)

## Prerequisites

- .NET 8 SDK (the Lambda functions target `net8.0` even if a later SDK is installed).
- Node.js 20+ and npm.
- AWS CLI v2, configured (`aws configure`) with an IAM user that has permissions
  to deploy the resources in this stack.
- AWS CDK CLI: `npm install -g aws-cdk`.
- **No Docker required.** Lambda packaging runs `dotnet publish -r linux-x64
  --self-contained false` directly on the host (see
  `infra/VerificationEngine.Infra/DotnetLambdaAsset.cs`) rather than through
  CDK's Docker-based asset bundling.

## One-time AWS account setup (do this first, before anything else)

1. **Billing alert**: AWS Console → Billing → Budgets → create budgets at
   $10 and $50. This is a manual console step by design — it should exist
   before any resource that could possibly cost money does.
2. **IAM user**: AWS Console → IAM → Users → create a user with
   programmatic access and the permissions needed to deploy CDK stacks
   (or `AdministratorAccess` for a personal/demo account). Run
   `aws configure` with its access key.
3. **CDK bootstrap** (one-time per account/region):
   ```bash
   cd infra/VerificationEngine.Infra
   cdk bootstrap
   ```

## Deploying the stack

The stack needs one required context value (the SES sender address) and one
optional one (the frontend's origin, for CORS — you won't know the Amplify
domain until after the first deploy, so the first deploy uses the default
`http://localhost:5173` and a second deploy tightens it once you know the
real domain).

```bash
cd infra/VerificationEngine.Infra
cdk deploy -c senderEmail=you@example.com
```

This deploys: Cognito, the DynamoDB table, the S3 documents bucket, the API
Gateway HTTP API + Lambda, the Step Functions state machine + its task
Lambdas, the SQS notification queue, the SES email identity, and the Amplify
Hosting app shell (see `VerificationEngineStack.cs` and its partial-class
files for the full breakdown — 45 resources in total, none of them RDS, VPC,
NAT, WAF, or a customer-managed KMS key).

Note the `Outputs` printed at the end (`ApiUrl`, `UserPoolId`,
`UserPoolClientId`, `AmplifyAppId`, ...) — you'll need them below.

## Finish SES (required before any email will actually send)

SES stays in **sandbox mode** deliberately (requesting production access is
for real send volume, not a demo, and sandbox is free either way). In
sandbox, every recipient must be individually verified, not just the sender:

1. AWS Console → SES → Verified identities → confirm the sender address
   received and clicked its verification email (`cdk deploy` triggers this
   automatically, but can't click the link for you).
2. For every email address you'll test with (your own claimant/executor test
   accounts), add and verify it the same way.

## Connect Amplify Hosting to GitHub (the one manual console step)

The CDK stack creates the Amplify **App** and its `main` **Branch**, but not
the GitHub connection itself. Amplify's current, non-deprecated way to
connect a repo is its GitHub App integration, which requires an interactive
OAuth authorization in the console — there's no way to script this step
(the older personal-access-token flow CDK's constructs still technically
support is the flow GitHub itself has deprecated for new connections).

1. Push this repo to GitHub if you haven't already.
2. AWS Console → Amplify → find the app (`AmplifyAppId` from the CDK output)
   → "main" branch → connect it to your GitHub repository, authorizing
   Amplify's GitHub App when prompted.
3. Trigger a build (or push a commit) — it runs the build spec baked into
   the app by CDK (`npm ci && npm run build` inside `frontend/`, artifact
   `frontend/dist`).

## Wire up the frontend's own config for local development

```bash
cd frontend
cp .env.example .env.local
```

Fill in `.env.local` from the `cdk deploy` outputs (`ApiUrl` →
`VITE_API_BASE_URL`, `UserPoolId` → `VITE_COGNITO_USER_POOL_ID`,
`UserPoolClientId` → `VITE_COGNITO_CLIENT_ID`, plus your region). Amplify
Hosting sets the equivalent build-time environment variables itself — see
`VerificationEngineStack.Frontend.cs` — so this file only matters for
`npm run dev`.

## Tightening CORS after the first deploy

Once you have the real Amplify domain (`https://main.xxxxxxxxxx.amplifyapp.com`),
redeploy with it so the API and S3 bucket only accept that origin instead of
`localhost`:

```bash
cdk deploy -c senderEmail=you@example.com -c frontendBaseUrl=https://main.xxxxxxxxxx.amplifyapp.com
```

## Tearing it down

```bash
cd infra/VerificationEngine.Infra
cdk destroy -c senderEmail=you@example.com
```

The DynamoDB table, S3 bucket, and Cognito user pool all have
`RemovalPolicy.DESTROY` (and the bucket has `autoDeleteObjects`) — this is a
demo dataset, so teardown removes everything rather than leaving an orphaned,
still-billing resource behind.
