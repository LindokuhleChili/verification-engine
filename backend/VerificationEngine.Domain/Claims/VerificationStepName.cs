namespace VerificationEngine.Domain.Claims;

/// <summary>
/// The named checkpoints a claim passes through. Which subset applies depends on
/// <see cref="ClaimType"/> — see <see cref="ClaimWorkflow"/>.
/// </summary>
public enum VerificationStepName
{
    IdentityBiometric,
    BankAccount,
    DocumentExtraction,
    ExecutorIdentity,
    CourierDelivery,
    Signature
}
