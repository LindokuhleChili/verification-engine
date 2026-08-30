# Verification Engine

I built this to show what I can actually do with AWS, end to end: not just wiring up a few services, but designing the data model, getting the infrastructure fully into code, and debugging it live once real traffic hit it. It's a full-stack app that lets South African shareholders claim unpaid dividends and dormant or lost shares without the usual paper trail: certified ID copies, stamped bank letters, an in-person affidavit. All of that gets replaced with biometric identity checks, OCR document extraction, open-banking-style verification, and e-signatures.

The point of building it was to demonstrate, with a working deployed system rather than slides: infrastructure fully defined as code (AWS CDK, C#), a real AWS-native identity and document pipeline (Cognito, Rekognition, Textract), and event-driven serverless orchestration (Step Functions, EventBridge, SQS), all while keeping AWS spend at effectively $0.

See [`docs/project-brief.md`](docs/project-brief.md) for the original scope and constraints this was built against, and [`docs/design.md`](docs/design.md) for the design system every screen follows.

## Three claim types

1. **Living Shareholder**: a shareholder whose bank or address changed, claiming dividends.
2. **Deceased Estate**: a beneficiary and an executor jointly transferring a deceased shareholder's shares.
3. **Lost Paper Certificate**: a shareholder replacing a lost certificate, with a legal indemnity affidavit.

## What's real vs. what's simulated

Every third-party integration sits behind a named interface
(`IStitchClient`, `IHomeAffairsClient`, `ICourierClient`, ...) so the
swap-in point for a real vendor is a single class. This is deliberate and
documented, not hidden:

| Capability | Status | Why |
|---|---|---|
| Face comparison (selfie vs. ID document) | **Real**: Amazon Rekognition `CompareFaces` | Genuine biometric match, real confidence threshold |
| Document OCR (Master's Reference Number, etc.) | **Real**: Amazon Textract `AnalyzeDocument` | Genuine extraction with per-field confidence |
| PDF generation (SARS form, CM42, affidavit) | **Real**: QuestPDF, rendered server-side | |
| Email notifications | **Real**: Amazon SES (sandbox mode) | Replaces WhatsApp per the project brief |
| Infrastructure | **Real**: 100% AWS CDK (C#), nothing clicked except one console step (see [`docs/DEPLOYING.md`](docs/DEPLOYING.md)) | |
| Home Affairs population-register lookup | Simulated (`MockHomeAffairsClient`) | No public DHA API exists outside accredited financial-institution access |
| Open banking (Stitch) | Simulated (`MockStitchClient`) | Requires a commercial contract and real bank credentials |
| Bank account verification (TransUnion) | Simulated | Paid commercial API |
| Advanced electronic signature (LawTrust) | Partly simulated | A real signature is captured and SHA-256 hashed with a timestamp: a genuine audit trail, just not an accredited ECTA signature |
| Master's Office reference validation | Format-checked only | No public validation API exists |
| Courier tracking (Courier Guy) | Simulated (`MockCourierClient`) | Paid commercial API |
| Money movement | Never happens | This is a demo |

Every simulated response is prefixed `[SIMULATED]` wherever it reaches the
UI or a generated document, so a demo is never mistaken for the real thing.

## Repository structure

```
backend/
  VerificationEngine.Domain/     Plain domain model: claims, steps, documents (no AWS SDK dependency)
  VerificationEngine.Services/   AWS-backed services + mocked vendor clients, single-table DynamoDB repository
  VerificationEngine.Api/        ASP.NET Core minimal API, hosted as one Lambda behind API Gateway
  VerificationEngine.Workers/    Step Functions task Lambdas + the EventBridge/SQS notification consumer
  VerificationEngine.Tests/      xUnit tests for the parts with real logic (SA ID validation, workflow rules, mocks)
infra/
  VerificationEngine.Infra/      The entire AWS stack, as CDK (C#), see below
frontend/
  React + Vite + Tailwind, ported from the Stitch-generated screens in docs/stitch-screens/
docs/
  project-brief.md, design.md, design-tokens.md, stitch-screens/   Original design artefacts
  DEPLOYING.md                   How to actually deploy this
```

## Architecture

- **Auth**: Cognito User Pool, email/password, JWT validated by API Gateway before Lambda ever runs.
- **API**: One Lambda (ASP.NET Core minimal API) behind an HTTP API Gateway with a Cognito JWT authoriser.
- **Data**: One DynamoDB table, single-table design. A claim, its verification steps, and its uploaded documents all share one partition, so loading a full claim is one `Query`. See `VerificationEngine.Domain.Persistence.TableKeys` for the key schema.
- **Files**: One private S3 bucket. All access is via short-lived presigned URLs, nothing is ever public.
- **Orchestration**: A Step Functions state machine polls (Wait/Choice loop) until both parties on a Deceased Estate claim are verified and the courier delivery is confirmed, then marks the claim Verified, or, after a bounded number of attempts, ActionNeeded.
- **Messaging**: EventBridge decouples "a claim was submitted" from "someone gets emailed about it". The API publishes an event and returns immediately; a queue-backed Lambda (with a dead-letter queue) sends the actual email.

## Cost guardrails

No RDS/Aurora, no VPC/NAT Gateway, no customer-managed KMS keys, no Secrets
Manager (SSM Parameter Store instead), no WAF, no SNS SMS. Every service
used sits inside AWS's free tier at demo-level traffic. See
[`docs/DEPLOYING.md`](docs/DEPLOYING.md) for the billing-alert setup this
was built against.

## Local development

```bash
# Backend
dotnet build VerificationEngine.slnx
dotnet test backend/VerificationEngine.Tests

# Frontend
cd frontend
npm install
cp .env.example .env.local   # fill in values from `cdk deploy` outputs
npm run dev
```

## Deploying

See [`docs/DEPLOYING.md`](docs/DEPLOYING.md).
