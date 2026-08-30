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

## Create Amplify Hosting via the console (the one manual step)

Amplify Hosting is the one piece of this project **not** defined in CDK.
An earlier version of the stack did create the Amplify App and Branch as
CDK resources (`CfnApp`/`CfnBranch`), but that turned out to be a dead end:
Amplify's current, non-deprecated way to connect a repo is its GitHub App
integration, and that integration can only be attached to an app **at
creation time**, through Amplify's own "New app" console wizard — there is
no way to attach it to a pre-existing app afterward, whether that app was
created by CDK or anything else. (The older personal-access-token flow that
`CfnApp` still technically supports is the flow GitHub has deprecated for
new connections, so it isn't a real alternative either.)

So: create it directly in the console.

1. Push this repo to GitHub if you haven't already.
2. AWS Console → Amplify → **New app** → **Host a web app** (or similar
   wording — this has been worded a few different ways across console
   versions) → choose **GitHub** as the source, authorize the AWS Amplify
   GitHub App if prompted, and select this repository and its branch.
3. Since this repo is a monorepo (frontend/backend/infra all at the top
   level), check **"My app is a monorepo"** and set the app root to
   `frontend`.
4. Accept the auto-detected build settings (`npm run build`, output
   directory `dist`) — Amplify detects these correctly for a Vite app once
   the app root is set.
5. Under **Advanced settings → Environment variables**, add:
   - `VITE_API_BASE_URL` → the `ApiUrl` CDK output
   - `VITE_COGNITO_USER_POOL_ID` → the `UserPoolId` CDK output
   - `VITE_COGNITO_CLIENT_ID` → the `UserPoolClientId` CDK output
   - `VITE_AWS_REGION` → your deploy region
6. Leave "Password protect my site", "Keep cookies in cache key", and SSR
   options off — this is a plain static SPA.
7. Save and deploy. Once it finishes, the app is live at
   `https://<branch>.<appId>.amplifyapp.com`.
8. Redeploy the CDK stack with that real domain as `frontendBaseUrl` (see
   below) so CORS on the API and S3 bucket is scoped to it instead of
   `localhost`.

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
