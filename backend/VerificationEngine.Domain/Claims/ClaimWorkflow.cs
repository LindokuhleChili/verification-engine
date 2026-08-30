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

    /// <summary>
    /// A claim may be submitted once every required step except signing has passed.
    /// Signature is deliberately excluded: submitting is what triggers the async side
    /// of verification (the notification queue, and for a Deceased Estate claim, the
    /// Step Functions workflow that waits on the second party and the courier) - it has
    /// to be possible to submit *before* signing, or that orchestration would only ever
    /// start after the claimant already signed, which is backwards. The actual gate
    /// against signing too early lives independently in the signature endpoint itself
    /// (see VerificationEndpoints.SubmitSignature), which checks every other required
    /// step directly rather than relying on this claim-level flag.
    /// </summary>
    public static bool CanSubmit(ClaimType claimType, IReadOnlyDictionary<VerificationStepName, VerificationStepStatus> actual) =>
        RequiredSteps(claimType)
            .Where(step => step != VerificationStepName.Signature)
            .All(step => actual.TryGetValue(step, out var status) && status == VerificationStepStatus.Passed);
}
