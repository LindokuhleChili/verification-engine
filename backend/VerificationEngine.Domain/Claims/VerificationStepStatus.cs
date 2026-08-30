namespace VerificationEngine.Domain.Claims;

public enum VerificationStepStatus
{
    NotStarted,
    InProgress,
    Passed,
    Failed,
    /// <summary>Waiting on a human outside the system (the executor, a courier, a stamped page coming back).</summary>
    AwaitingCounterparty
}
