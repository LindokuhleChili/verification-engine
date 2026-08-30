namespace VerificationEngine.Domain.Claims;

public enum ClaimStatus
{
    /// <summary>Created but the claimant has not finished the wizard.</summary>
    Draft,

    /// <summary>Submitted; automated verification is running or awaiting a second party.</summary>
    Pending,

    /// <summary>Every required verification step passed.</summary>
    Verified,

    /// <summary>A step failed in a way the claimant can fix (bad selfie, unreadable document).</summary>
    ActionNeeded,

    /// <summary>Verified and the final document has been generated and stored.</summary>
    Complete,

    /// <summary>Rejected in a way the claimant cannot self-correct.</summary>
    Rejected
}
