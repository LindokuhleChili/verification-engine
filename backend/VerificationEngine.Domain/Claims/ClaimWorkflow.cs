namespace VerificationEngine.Domain.Claims;

/// <summary>
/// Single source of truth for which steps each claim type requires. Both the API
/// (to decide whether a claim may be submitted) and the frontend (to draw the step
/// indicator) derive from this, so the wizard can never drift from the rules.
/// </summary>
public static class ClaimWorkflow
{
    private static readonly IReadOnlyDictionary<ClaimType, VerificationStepName[]> Steps =
        new Dictionary<ClaimType, VerificationStepName[]>
        {
            [ClaimType.LivingShareholder] =
            [
                VerificationStepName.IdentityBiometric,
                VerificationStepName.BankAccount,
                VerificationStepName.Signature
            ],

            // The executor is a second human being who must verify separately, and the
            // original Letter of Executorship travels physically — hence the courier step.
            [ClaimType.DeceasedEstate] =
            [
                VerificationStepName.IdentityBiometric,
                VerificationStepName.DocumentExtraction,
                VerificationStepName.ExecutorIdentity,
                VerificationStepName.CourierDelivery,
                VerificationStepName.Signature
            ],

            // No bank step: dematerialising a certificate moves shares to a broker
            // account, it does not pay money out.
            [ClaimType.LostCertificate] =
            [
                VerificationStepName.IdentityBiometric,
                VerificationStepName.DocumentExtraction,
                VerificationStepName.Signature
            ]
        };

    public static IReadOnlyList<VerificationStepName> RequiredSteps(ClaimType claimType) => Steps[claimType];

    /// <summary>Human-readable label for the step indicator. Kept next to the workflow so the two stay in sync.</summary>
    public static string Label(VerificationStepName step) => step switch
    {
        VerificationStepName.IdentityBiometric => "Identity",
        VerificationStepName.BankAccount => "Banking",
        VerificationStepName.DocumentExtraction => "Documents",
        VerificationStepName.ExecutorIdentity => "Executor",
        VerificationStepName.CourierDelivery => "Courier",
        VerificationStepName.Signature => "Sign",
        _ => step.ToString()
    };

    /// <summary>A claim may only be submitted once every required step has passed.</summary>
    public static bool CanSubmit(ClaimType claimType, IReadOnlyDictionary<VerificationStepName, VerificationStepStatus> actual) =>
        RequiredSteps(claimType).All(step =>
            actual.TryGetValue(step, out var status) && status == VerificationStepStatus.Passed);
}
