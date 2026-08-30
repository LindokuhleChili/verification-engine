using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Services.Documents;

/// <summary>Picks the one generator registered for a claim's type. One per <see cref="ClaimType"/>, enforced at startup.</summary>
public sealed class ClaimDocumentGeneratorFactory
{
    private readonly IReadOnlyList<IClaimDocumentGenerator> _generators;

    public ClaimDocumentGeneratorFactory(IEnumerable<IClaimDocumentGenerator> generators) =>
        _generators = generators.ToList();

    public IClaimDocumentGenerator For(ClaimType claimType) =>
        _generators.SingleOrDefault(g => g.CanGenerate(claimType))
        ?? throw new InvalidOperationException($"No document generator is registered for claim type '{claimType}'.");
}
