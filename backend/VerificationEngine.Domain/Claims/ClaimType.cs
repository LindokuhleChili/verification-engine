namespace VerificationEngine.Domain.Claims;

/// <summary>
/// The three legally distinct routes a claimant can take. Each one collects different
/// evidence and produces a different final document, but they share the same
/// identity-verification and document-storage plumbing.
/// </summary>
public enum ClaimType
{
    /// <summary>Shareholder of record whose address or bank details changed. Produces a SARS dividend tax declaration.</summary>
    LivingShareholder,

    /// <summary>Beneficiary and executor jointly claiming a deceased shareholder's holding. Produces a CM42 transfer form.</summary>
    DeceasedEstate,

    /// <summary>Shareholder who lost a physical certificate and needs it dematerialised. Produces an indemnity affidavit.</summary>
    LostCertificate
}
