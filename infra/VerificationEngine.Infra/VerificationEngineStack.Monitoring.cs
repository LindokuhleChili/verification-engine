using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.StepFunctions;

namespace VerificationEngine.Infra;

public sealed partial class VerificationEngineStack
{
    /// <summary>
    /// One dashboard and a handful of alarms - the "CloudWatch + X-Ray" line from the
    /// original tech stack. X-Ray itself needs no CDK resource of its own: every Lambda
    /// in this stack has <c>Tracing = Tracing.ACTIVE</c> set (see BuildApiFunction,
    /// BuildMessaging, BuildDeceasedEstateWorkflow), which is what actually turns
    /// tracing on and grants the IAM permissions to write trace data; this method is
    /// only the CloudWatch half.
    ///
    /// Alarms are kept to the handful that would genuinely mean "a human should look at
    /// this" for a project at this scale, rather than one per resource: API-level
    /// errors, the API Lambda erroring, a notification permanently failing (landing in
    /// the dead-letter queue), and the Deceased Estate workflow failing outright.
    /// </summary>
    private void BuildMonitoring(
        Function apiFunction, Function notifierFunction, Queue notificationQueue, Queue notificationDlq,
        StateMachine stateMachine, string httpApiId)
    {
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps
        {
            TopicName = "verification-engine-alarms"
        });
        // A one-time confirmation email goes to this address, separate from (and in
        // addition to) the SES sender verification - SNS and SES are unrelated services
        // that each independently need consent before they'll send to an address.
        alarmTopic.AddSubscription(new EmailSubscription(SenderEmailAddress));

        var api5xxError = new Metric(new MetricProps
        {
            Namespace = "AWS/ApiGateway",
            MetricName = "5xxError",
            DimensionsMap = new Dictionary<string, string> { ["ApiId"] = httpApiId },
            Statistic = "Sum",
            Period = Duration.Minutes(5)
        });

        var api4xxError = new Metric(new MetricProps
        {
            Namespace = "AWS/ApiGateway",
            MetricName = "4xxError",
            DimensionsMap = new Dictionary<string, string> { ["ApiId"] = httpApiId },
            Statistic = "Sum",
            Period = Duration.Minutes(5)
        });

        var apiCount = new Metric(new MetricProps
        {
            Namespace = "AWS/ApiGateway",
            MetricName = "Count",
            DimensionsMap = new Dictionary<string, string> { ["ApiId"] = httpApiId },
            Statistic = "Sum",
            Period = Duration.Minutes(5)
        });

        AddAlarm("Api5xxErrors", "The API returned a server error - check the ApiFunction logs.",
            api5xxError, alarmTopic);
        AddAlarm("ApiFunctionErrors", "The API Lambda threw an unhandled exception.",
            apiFunction.MetricErrors(new MetricOptions { Period = Duration.Minutes(5) }), alarmTopic);
        AddAlarm("NotificationDeadLetters", "A claim notification permanently failed after 3 retries and landed on the dead-letter queue.",
            notificationDlq.MetricApproximateNumberOfMessagesVisible(), alarmTopic);
        AddAlarm("DeceasedEstateWorkflowFailures", "The Deceased Estate verification workflow failed outright (not just timed out waiting).",
            stateMachine.MetricFailed(new MetricOptions { Period = Duration.Minutes(5) }), alarmTopic);

        _ = new Dashboard(this, "Dashboard", new DashboardProps
        {
            DashboardName = "verification-engine",
            Widgets =
            [
                [
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "API Gateway - requests & errors",
                        Left = [apiCount, api4xxError, api5xxError],
                        Width = 12
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "API Lambda - invocations, errors, duration",
                        Left = [apiFunction.MetricInvocations(), apiFunction.MetricErrors()],
                        Right = [apiFunction.MetricDuration()],
                        Width = 12
                    })
                ],
                [
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "DynamoDB - consumed capacity",
                        Left = [_table.MetricConsumedReadCapacityUnits(), _table.MetricConsumedWriteCapacityUnits()],
                        Width = 12
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Notification queue - depth & dead letters",
                        Left = [notificationQueue.MetricApproximateNumberOfMessagesVisible()],
                        Right = [notificationDlq.MetricApproximateNumberOfMessagesVisible()],
                        Width = 12
                    })
                ],
                [
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Deceased Estate workflow - executions",
                        Left = [stateMachine.MetricStarted(), stateMachine.MetricSucceeded(), stateMachine.MetricFailed()],
                        Width = 12
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Claim notifier - invocations & errors",
                        Left = [notifierFunction.MetricInvocations(), notifierFunction.MetricErrors()],
                        Width = 12
                    })
                ]
            ]
        });
    }

    private void AddAlarm(string id, string description, IMetric metric, Topic topic)
    {
        var alarm = new Alarm(this, id, new AlarmProps
        {
            AlarmName = $"verification-engine-{id}",
            AlarmDescription = description,
            Metric = metric,
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        alarm.AddAlarmAction(new SnsAction(topic));
    }
}
