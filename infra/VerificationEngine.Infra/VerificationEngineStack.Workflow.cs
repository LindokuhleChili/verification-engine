using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;

namespace VerificationEngine.Infra;

public sealed partial class VerificationEngineStack
{
    /// <summary>Retry budget for the poll loop below - see the Wait/Choice comment for why this number.</summary>
    private const int MaxPollAttempts = 5;

    /// <summary>
    /// Orchestrates the one genuinely multi-party, multi-step flow in this project: a
    /// Deceased Estate claim needs the beneficiary verified, the executor verified
    /// (a second human, invited separately - see ExecutorEndpoints), and the original
    /// Letter of Executorship physically delivered, before the joint CM42 can be signed.
    ///
    /// Modeled as poll-and-wait rather than each step directly resuming the state
    /// machine (e.g. via a callback token) because the three things it is waiting on
    /// are triggered from three unrelated API calls made by two different people at
    /// unpredictable times - polling a cheap read is simpler to reason about here than
    /// wiring a task token through every one of those endpoints for a project this size.
    /// </summary>
    private StateMachine BuildDeceasedEstateWorkflow(IEventBus eventBus)
    {
        var checkParties = new Function(this, "CheckEstatePartiesFunction", new FunctionProps
        {
            FunctionName = "verification-engine-check-estate-parties",
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.X86_64,
            MemorySize = 256,
            Tracing = Tracing.ACTIVE,
            Timeout = Duration.Seconds(10),
            Handler = "VerificationEngine.Workers::VerificationEngine.Workers.Functions.CheckEstatePartiesVerifiedFunction::FunctionHandler",
            Code = DotnetLambdaAsset.FromProject("VerificationEngine.Workers"),
            Environment = WorkerEnvironment()
        });
        _table.GrantReadData(checkParties);

        var markVerified = new Function(this, "MarkEstateVerifiedFunction", new FunctionProps
        {
            FunctionName = "verification-engine-mark-estate-verified",
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.X86_64,
            MemorySize = 256,
            Tracing = Tracing.ACTIVE,
            Timeout = Duration.Seconds(10),
            Handler = "VerificationEngine.Workers::VerificationEngine.Workers.Functions.MarkEstateClaimVerifiedFunction::FunctionHandler",
            Code = DotnetLambdaAsset.FromProject("VerificationEngine.Workers"),
            Environment = WorkerEnvironment()
        });
        _table.GrantReadWriteData(markVerified);
        GrantSesSendAccess(markVerified);

        var markActionNeeded = new Function(this, "MarkEstateActionNeededFunction", new FunctionProps
        {
            FunctionName = "verification-engine-mark-estate-action-needed",
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.X86_64,
            MemorySize = 256,
            Tracing = Tracing.ACTIVE,
            Timeout = Duration.Seconds(10),
            Handler = "VerificationEngine.Workers::VerificationEngine.Workers.Functions.MarkEstateClaimActionNeededFunction::FunctionHandler",
            Code = DotnetLambdaAsset.FromProject("VerificationEngine.Workers"),
            Environment = WorkerEnvironment()
        });
        _table.GrantReadWriteData(markActionNeeded);
        GrantSesSendAccess(markActionNeeded);

        var checkPartiesTask = new LambdaInvoke(this, "CheckPartiesTask", new LambdaInvokeProps
        {
            LambdaFunction = checkParties,
            // The function's whole return value becomes the state's output (no SDK
            // response envelope, no merge with the prior input) - the Output record
            // already carries every field the next Choice/task needs.
            PayloadResponseOnly = true
        });

        var markVerifiedTask = new LambdaInvoke(this, "MarkVerifiedTask", new LambdaInvokeProps
        {
            LambdaFunction = markVerified,
            PayloadResponseOnly = true
        });

        var markActionNeededTask = new LambdaInvoke(this, "MarkActionNeededTask", new LambdaInvokeProps
        {
            LambdaFunction = markActionNeeded,
            PayloadResponseOnly = true
        });

        // Five attempts at a five-minute wait is 25 minutes of genuine patience for two
        // separate humans (the beneficiary and the executor) plus a real courier
        // delivery before this asks a person to look at it - long enough to not be
        // trigger-happy, short enough to keep a demo's feedback loop reasonable.
        var wait = new Wait(this, "WaitBeforeRecheck", new WaitProps
        {
            Time = WaitTime.Duration(Duration.Minutes(5))
        });

        var succeedVerified = new Succeed(this, "EstateVerified");
        var succeedActionNeeded = new Succeed(this, "EstateNeedsAction");

        var choice = new Choice(this, "AllPartiesVerified?")
            .When(Condition.BooleanEquals("$.AllVerified", true), markVerifiedTask.Next(succeedVerified))
            .When(Condition.NumberGreaterThanEquals("$.Attempt", MaxPollAttempts), markActionNeededTask.Next(succeedActionNeeded))
            .Otherwise(wait.Next(checkPartiesTask));

        var definition = checkPartiesTask.Next(choice);

        var stateMachine = new StateMachine(this, "DeceasedEstateStateMachine", new StateMachineProps
        {
            StateMachineName = "verification-engine-deceased-estate",
            DefinitionBody = DefinitionBody.FromChainable(definition),
            StateMachineType = StateMachineType.STANDARD,
            // Standard's per-state-transition free tier (4,000/month, forever) comfortably
            // covers this: one run is at most 2 + (2 x MaxPollAttempts) transitions.
            Timeout = Duration.Hours(1)
        });

        _ = new Rule(this, "ClaimSubmittedToStateMachineRule", new RuleProps
        {
            RuleName = "verification-engine-deceased-estate-submitted",
            EventBus = eventBus,
            EventPattern = new EventPattern
            {
                Source = ["verification-engine.claims"],
                DetailType = ["ClaimSubmitted"],
                // Matches the exact casing EventBridgeClaimEventPublisher emits: the
                // anonymous object's C# property names, serialized with no naming
                // policy applied - see ClaimsEndpoints.SubmitClaim.
                Detail = new Dictionary<string, object> { ["ClaimType"] = new[] { "DeceasedEstate" } }
            },
            Targets =
            [
                new SfnStateMachine(stateMachine, new SfnStateMachineProps
                {
                    Input = RuleTargetInput.FromObject(new Dictionary<string, object>
                    {
                        ["ClaimId"] = EventField.FromPath("$.detail.ClaimId"),
                        ["Attempt"] = 0
                    })
                })
            ]
        });

        return stateMachine;
    }

    private Dictionary<string, string> WorkerEnvironment() => new()
    {
        ["TABLE_NAME"] = _table.TableName,
        ["DOCUMENTS_BUCKET"] = _documentsBucket.BucketName,
        ["SENDER_EMAIL"] = SenderEmailAddress,
        ["FRONTEND_BASE_URL"] = FrontendBaseUrl
    };
}
