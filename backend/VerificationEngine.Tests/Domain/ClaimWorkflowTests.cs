using VerificationEngine.Domain.Claims;
using Xunit;

namespace VerificationEngine.Tests.Domain;

public sealed class ClaimWorkflowTests
{
    [Fact]
    public void LivingShareholder_DoesNotRequireDocumentExtractionOrCourier()
    {
        var steps = ClaimWorkflow.RequiredSteps(ClaimType.LivingShareholder);

        Assert.DoesNotContain(VerificationStepName.DocumentExtraction, steps);
        Assert.DoesNotContain(VerificationStepName.CourierDelivery, steps);
        Assert.DoesNotContain(VerificationStepName.ExecutorIdentity, steps);
    }

    [Fact]
    public void DeceasedEstate_RequiresBothPartiesAndCourierButNoBankStep()
    {
        var steps = ClaimWorkflow.RequiredSteps(ClaimType.DeceasedEstate);

        Assert.Contains(VerificationStepName.IdentityBiometric, steps);
        Assert.Contains(VerificationStepName.ExecutorIdentity, steps);
        Assert.Contains(VerificationStepName.CourierDelivery, steps);
        // Dematerialising into a broker account moves shares, not money - no bank link needed.
        Assert.DoesNotContain(VerificationStepName.BankAccount, steps);
    }

    [Fact]
    public void LostCertificate_RequiresExtractionButNoBankOrCourierStep()
    {
        var steps = ClaimWorkflow.RequiredSteps(ClaimType.LostCertificate);

        Assert.Contains(VerificationStepName.DocumentExtraction, steps);
        Assert.DoesNotContain(VerificationStepName.BankAccount, steps);
        Assert.DoesNotContain(VerificationStepName.CourierDelivery, steps);
    }

    [Fact]
    public void CanSubmit_FalseWhenAnyRequiredStepIsMissing()
    {
        var actual = new Dictionary<VerificationStepName, VerificationStepStatus>
        {
            [VerificationStepName.IdentityBiometric] = VerificationStepStatus.Passed,
            [VerificationStepName.BankAccount] = VerificationStepStatus.InProgress
            // Signature step is entirely absent.
        };

        Assert.False(ClaimWorkflow.CanSubmit(ClaimType.LivingShareholder, actual));
    }

    [Fact]
    public void CanSubmit_TrueWhenEveryStepExceptSignaturePassed()
    {
        var actual = new Dictionary<VerificationStepName, VerificationStepStatus>
        {
            [VerificationStepName.IdentityBiometric] = VerificationStepStatus.Passed,
            [VerificationStepName.BankAccount] = VerificationStepStatus.Passed
            // Signature step is entirely absent - submitting is what should trigger
            // verification, not something that waits until after signing.
        };

        Assert.True(ClaimWorkflow.CanSubmit(ClaimType.LivingShareholder, actual));
    }

    [Fact]
    public void CanSubmit_IgnoresSignatureEvenIfAlreadyPassed()
    {
        var actual = new Dictionary<VerificationStepName, VerificationStepStatus>
        {
            [VerificationStepName.IdentityBiometric] = VerificationStepStatus.Passed,
            [VerificationStepName.BankAccount] = VerificationStepStatus.Passed,
            [VerificationStepName.Signature] = VerificationStepStatus.Passed
        };

        Assert.True(ClaimWorkflow.CanSubmit(ClaimType.LivingShareholder, actual));
    }

    [Fact]
    public void CanSubmit_FalseWhenAStepFailedRatherThanPassed()
    {
        var actual = new Dictionary<VerificationStepName, VerificationStepStatus>
        {
            [VerificationStepName.IdentityBiometric] = VerificationStepStatus.Failed,
            [VerificationStepName.BankAccount] = VerificationStepStatus.Passed,
            [VerificationStepName.Signature] = VerificationStepStatus.Passed
        };

        Assert.False(ClaimWorkflow.CanSubmit(ClaimType.LivingShareholder, actual));
    }
}
