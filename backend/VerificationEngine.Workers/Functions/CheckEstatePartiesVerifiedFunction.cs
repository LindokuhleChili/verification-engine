using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Services;
using VerificationEngine.Services.Persistence;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VerificationEngine.Workers.Functions;

/// <summary>
/// A Task state in the Deceased Estate Step Functions state machine. Polled on a Wait
/// loop after the claim is submitted: reports whether the beneficiary, the executor,
/// and the courier delivery of the physical Letter of Executorship have all completed,
/// so the state machine's Choice state knows whether to keep waiting, mark the claim
/// verified, or - after too many attempts - flag it for human attention.
/// </summary>
public sealed class CheckEstatePartiesVerifiedFunction
{
    private readonly IClaimRepository _repository;

    public CheckEstatePartiesVerifiedFunction()
    {
        var services = new ServiceCollection().AddVerificationEngineServices().BuildServiceProvider();
        _repository = services.GetRequiredService<IClaimRepository>();
    }

    public sealed record Input(string ClaimId, int Attempt);

    public sealed record Output(
        string ClaimId, int Attempt,
        bool BeneficiaryVerified, bool ExecutorVerified, bool CourierDelivered, bool AllVerified);

    public async Task<Output> FunctionHandler(Input input, ILambdaContext context)
    {
        var aggregate = await _repository.GetClaimAggregateAsync(input.ClaimId);
        if (aggregate is null)
            throw new InvalidOperationException($"Claim {input.ClaimId} was not found - the state machine should not have started for it.");

        bool Passed(VerificationStepName name) =>
            aggregate.Steps.Any(s => s.Name == name && s.Status == VerificationStepStatus.Passed);

        var beneficiaryVerified = Passed(VerificationStepName.IdentityBiometric);
        var executorVerified = aggregate.Steps.Any(s =>
            s.Name == VerificationStepName.ExecutorIdentity && s.Status == VerificationStepStatus.Passed);
        var courierDelivered = aggregate.Steps.Any(s =>
            s.Name == VerificationStepName.CourierDelivery && s.Status == VerificationStepStatus.Passed);

        var allVerified = beneficiaryVerified && executorVerified && courierDelivered;

        context.Logger.LogInformation(
            $"Claim {input.ClaimId} attempt {input.Attempt}: beneficiary={beneficiaryVerified} executor={executorVerified} courier={courierDelivered}");

        return new Output(input.ClaimId, input.Attempt + 1, beneficiaryVerified, executorVerified, courierDelivered, allVerified);
    }
}
