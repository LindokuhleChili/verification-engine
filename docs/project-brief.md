# Verification Engine — Project Brief

**Read this whole document before writing any code.** This is a CV portfolio project built by two
developers comfortable with C#, on a 2-week deadline, using an AWS free-tier account with a $100
credit cap. Cost control is a hard constraint, not a nice-to-have — if a decision might cost real
money, stop and ask before proceeding rather than assuming it's fine.

## 1. What this product is

A web app that lets South African shareholders claim unpaid dividends and dormant/lost shares
without the usual manual paperwork (certified ID copies, stamped bank letters, in-person affidavit
stamps). It replaces that process with biometric identity checks, open-banking verification, OCR
document extraction, and e-signatures, and routes the user through one of three claim types:

1. **Living Shareholder** — a shareholder whose address/bank details changed, claiming dividends.
2. **Deceased Estate** — a beneficiary and an executor jointly claiming/transferring a deceased
   shareholder's shares.
3. **Lost Paper Certificate** — a shareholder who lost a physical share certificate and needs it
   digitized (dematerialised), including a legal indemnity affidavit.

Full legal/business detail for each scenario is in the attached scenario document — treat it as
the source of truth for *what* each flow legally needs to collect and produce, even though the
*how* below replaces several steps with AWS services.

## 2. Success criteria

- Deployed, working web app reachable by URL, demoable in an interview.
- Entire AWS spend stays near $0 (see cost guardrails — this matters as much as functionality).
- Infrastructure fully defined as code (AWS CDK, C#) — nothing clicked manually in the console
  except one-time account setup (billing alerts, IAM user).
- Code is clean enough to walk through in an interview: sensible project structure, meaningful
  naming, comments where a design decision isn't obvious.
- At the very end of the project (see Section 9), a beginner-friendly explanation document is
  produced — do not generate this early; it should describe the *finished* system.

## 3. Tech stack (fixed — do not substitute without asking)

- **Language**: C# (.NET 8) for all backend code (Lambda functions) and infrastructure (AWS CDK).
- **Frontend**: React, hosted via AWS Amplify Hosting.
- **Infra as Code**: AWS CDK (C# flavor).
- **Auth**: Amazon Cognito.
- **API**: API Gateway (HTTP API type).
- **Compute**: AWS Lambda (.NET 8 runtime).
- **Orchestration**: AWS Step Functions (for the multi-step/multi-party deceased-estate flow).
- **Messaging**: SQS + EventBridge.
- **Data**: DynamoDB (single-table design) — no RDS, no VPC, no NAT Gateway anywhere in this
  project.
- **File storage**: S3 (uploaded documents, generated PDFs).
- **AI services**: Amazon Rekognition (face comparison, replaces "Smile ID" biometric KYC) and
  Amazon Textract (OCR, replaces manual extraction of the Master's Reference Number from estate
  documents).
- **Email**: Amazon SES (replaces WhatsApp-link notifications — WhatsApp Business API is out of
  scope for this project).
- **PDF generation**: QuestPDF (C# library), run inside a Lambda function.
- **Monitoring**: CloudWatch + X-Ray.

## 4. Cost guardrails — non-negotiable

- No RDS, no Aurora (Serverless or otherwise), no VPC, no NAT Gateway.
- No customer-managed KMS keys — use AWS-managed keys (`aws/s3`, `aws/dynamodb`) for encryption
  at rest.
- No AWS Secrets Manager — use SSM Parameter Store (standard tier, free) for any stored keys.
- No AWS WAF.
- No SNS SMS — use SES email for all notifications.
- Real external vendors (Smile ID, Stitch, TransUnion, Twilio, LawTrust, Courier Guy) are **not**
  to be integrated for real — see Section 6, Mocking Strategy.
- Before creating any AWS resource not listed in Section 3, stop and ask.
- Set up an AWS Budget alert at $10 and $50 as the very first task, before any other resource is
  created.

## 5. Build order (2-week timeline — build in this priority order, not all-at-once)

1. **Foundation** (days 1–3): CDK app skeleton, Cognito user pool, API Gateway, DynamoDB tables,
   S3 buckets, one working "hello world" Lambda deployed end-to-end through CDK. Budget alert set
   up first.
2. **Scenario 1: Living Shareholder** (days 4–7) — build this fully end-to-end first, since it's
   the simplest full vertical slice and establishes the auth → upload → verify → generate-PDF
   pattern the other two scenarios reuse: sign up/log in → upload a selfie → Rekognition face
   compare against a mocked "Home Affairs" record → mocked Stitch bank-link confirmation →
   generate the SARS dividend tax form PDF via QuestPDF → store in S3 → show status on a
   dashboard.
3. **Scenario 3: Lost Paper Certificate** (days 8–11) — reuses most of Scenario 1's plumbing.
   Adds: dynamic indemnity form generation, and the "print & stamp fallback" UX path described in
   the scenario document (QR code linking a manually stamped document back to the digital claim).
4. **Scenario 2: Deceased Estate** (days 12–14, cut first if behind schedule) — the most complex:
   Step Functions orchestration, a second user (Executor) invited via a secure emailed link
   (SES, not WhatsApp), parallel verification of both parties, Textract extraction of the Master's
   Reference Number from the uploaded Letter of Executorship, joint CM42 form generation.

Do not start a scenario before the previous one in this order is working end-to-end and deployed.

## 6. Mocking strategy for third-party vendors

Build every integration exactly as if calling the real vendor (a clearly separated
service/interface per vendor: `ISmileIdClient`, `IStitchClient`, `ITwilioClient`, etc.), but the
implementation returns realistic, clearly-labeled fake responses instead of making a real network
call. Document this explicitly in the README so it's transparent, not misleading. Where a real AWS
service can stand in for a vendor's function (Rekognition for Smile ID, Textract for OCR), use the
real service — that's actual working functionality worth demoing, not a mock.

## 7. Design system

A design brief (`design.md`) and Google Stitch-generated screens exist separately — follow the
color tokens, typography, and component rules in that document exactly. Do not invent a different
visual direction. If a screen isn't covered by the Stitch output, extrapolate using the same
tokens rather than defaulting to a generic component library look.

## 8. What I need to provide you (ask me for these before you need them, don't block on all of them upfront)

- AWS account access (IAM user/role with the permissions needed for CDK deployment).
- GitHub repository for the code (for Amplify Hosting's CI/CD).
- Final Stitch-exported screen designs / design.md.
- Decisions on anything genuinely ambiguous in the scenario document that affects legal/compliance
  accuracy (I am not a lawyer, so flag these but keep moving with a reasonable assumption rather
  than blocking).
- Confirmation before creating any AWS resource outside the approved list in Section 3.
- My preference on repo structure if you're unsure (monorepo vs. separate infra/frontend repos) —
  default to monorepo if I don't answer.

## 9. Final deliverable — build this LAST, only once I confirm I'm satisfied

Once I explicitly confirm the project is complete and I'm happy with it, produce a separate
document: a **beginner-level, step-by-step manual** covering two things:

1. **How everything works** — walk through the finished architecture and codebase in plain
   language, screen by screen and service by service, as if explaining it to someone who has never
   used AWS or C# before. No assumed prior knowledge of Lambda, DynamoDB, CDK, etc. — define each
   term the first time it's used.
2. **How to run and deploy it** — a literal, numbered how-to: cloning the repo, installing
   prerequisites, configuring AWS credentials, running `cdk deploy`, running the frontend locally,
   and using the finished product as an end user.

Do not attempt to write this manual early or incrementally — it should describe the system as it
actually ended up, not as originally planned.

## 10. Working style expected throughout

- Ask before making architecture decisions not already fixed in this brief.
- Flag anything that risks incurring cost, even small amounts, before doing it.
- Keep me updated at the end of each build-order phase (Section 5) rather than going silent for
  days.
- If something in the original scenario document conflicts with the cost guardrails or tech
  stack here, this brief wins.
