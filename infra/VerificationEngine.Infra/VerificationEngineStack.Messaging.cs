using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SQS;

namespace VerificationEngine.Infra;

public sealed partial class VerificationEngineStack
{
    /// <summary>
    /// EventBridge decouples "a claim was submitted" from "someone got emailed about
    /// it": the API publishes the event and returns immediately (see
    /// EventBridgeClaimEventPublisher), and this queue-backed Lambda handles the
    /// notification on its own schedule, with its own retry behaviour.
    ///
    /// SQS sits between the rule and the Lambda specifically so a transient SES
    /// failure retries automatically and, after enough failures, lands on a
    /// dead-letter queue instead of the notification silently vanishing - a plain
    /// EventBridge-to-Lambda target would drop it after EventBridge's own limited
    /// retry policy expires, with less visibility into the failure.
    /// </summary>
    private IEventBus BuildMessaging(out Function notifierFunction)
    {
        var eventBus = Amazon.CDK.AWS.Events.EventBus.FromEventBusName(this, "DefaultBus", "default");

        var deadLetterQueue = new Queue(this, "ClaimNotificationsDlq", new QueueProps
        {
            QueueName = "verification-engine-claim-notifications-dlq",
            RetentionPeriod = Duration.Days(14)
        });

        var queue = new Queue(this, "ClaimNotificationsQueue", new QueueProps
        {
            QueueName = "verification-engine-claim-notifications",
            VisibilityTimeout = Duration.Seconds(30),
            DeadLetterQueue = new Amazon.CDK.AWS.SQS.DeadLetterQueue
            {
                Queue = deadLetterQueue,
                MaxReceiveCount = 3
            }
        });

        _ = new Rule(this, "ClaimSubmittedToQueueRule", new RuleProps
        {
            RuleName = "verification-engine-claim-submitted-notify",
            EventBus = eventBus,
            EventPattern = new EventPattern
            {
                Source = ["verification-engine.claims"],
                DetailType = ["ClaimSubmitted"]
            },
            Targets = [new SqsQueue(queue)]
        });

        notifierFunction = new Function(this, "ClaimSubmittedNotifierFunction", new FunctionProps
        {
            FunctionName = "verification-engine-claim-notifier",
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.X86_64,
            MemorySize = 256,
            Timeout = Duration.Seconds(15),
            Handler = "VerificationEngine.Workers::VerificationEngine.Workers.Functions.ClaimSubmittedNotifierFunction::FunctionHandler",
            Code = DotnetLambdaAsset.FromProject("VerificationEngine.Workers"),
            Environment = new Dictionary<string, string>
            {
                ["TABLE_NAME"] = _table.TableName,
                ["DOCUMENTS_BUCKET"] = _documentsBucket.BucketName,
                ["SENDER_EMAIL"] = SenderEmailAddress,
                ["FRONTEND_BASE_URL"] = FrontendBaseUrl
            }
        });

        notifierFunction.AddEventSource(new SqsEventSource(queue, new SqsEventSourceProps { BatchSize = 1 }));

        _table.GrantReadWriteData(notifierFunction);
        GrantSesSendAccess(notifierFunction);

        return eventBus;
    }
}
