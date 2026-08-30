namespace VerificationEngine.Domain.Verification;

/// <summary>
/// Outcome of the open-banking account check. Only the last four digits of the
/// account number are ever retained — full account numbers are not needed after
/// verification, and not storing them is the cheapest POPIA control available.
/// </summary>
public sealed record BankAccountVerificationResult(
    bool IsVerified,
    string BankName,
    string AccountLastFour,
    string AccountHolderName,
    string Detail);
