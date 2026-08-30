namespace VerificationEngine.Domain.Documents;

public enum DocumentType
{
    /// <summary>Live camera capture used as the probe image for face comparison.</summary>
    Selfie,

    /// <summary>Photo of the SA ID card or green book. Supplies the reference face and the ID number.</summary>
    IdDocument,

    /// <summary>Master's Office letter appointing the executor. Textract reads the reference number off it.</summary>
    LetterOfExecutorship,

    /// <summary>Abridged or unabridged death certificate.</summary>
    DeathCertificate,

    /// <summary>Anything the claimant still has relating to the lost share certificate.</summary>
    CertificateEvidence,

    /// <summary>Proof of residential address.</summary>
    ProofOfAddress
}
