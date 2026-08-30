using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Services.Documents;

/// <summary>
/// Produces the final PDF a claim type legally requires. REAL - QuestPDF renders
/// genuine, correctly laid out documents; the only "simulated" part is the e-signature
/// hash embedded in them (see <see cref="SignatureRecord"/>), because an accredited
/// advanced electronic signature is a paid, vetted service out of scope for this build.
/// </summary>
public interface IClaimDocumentGenerator
{
    /// <summary>True if this generator produces the document for the given claim type.</summary>
    bool CanGenerate(ClaimType claimType);

    byte[] Generate(ClaimDocumentRequest request);
}

/// <summary>Everything a generator needs, gathered from the claim aggregate by the caller.</summary>
public sealed record ClaimDocumentRequest(
    Claim Claim,
    SignatureRecord Signature,
    IReadOnlyDictionary<string, string> ExtraFields);

/// <summary>
/// A drawn signature is captured as an image and hashed rather than cryptographically
/// signed by an accredited provider (LawTrust and similar require paid vetting). The
/// hash plus timestamp is a genuine, independently-verifiable audit trail - just not a
/// legally accredited Advanced Electronic Signature.
/// </summary>
public sealed record SignatureRecord(string SignerName, string Sha256Hash, DateTimeOffset SignedAt);
