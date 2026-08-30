using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Api.Contracts;

public sealed record CreateClaimRequest(
    ClaimType ClaimType,
    string? ShareholderFullName,
    string? ShareholderIdNumber,
    string? CompanyName,
    long? AmountCents,
    string? CertificateNumber);

public sealed record ClaimSummaryResponse(
    string ClaimId,
    ClaimType ClaimType,
    ClaimStatus Status,
    string? CompanyName,
    long? AmountCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ClaimDetailResponse(
    string ClaimId,
    ClaimType ClaimType,
    ClaimStatus Status,
    string? ShareholderFullName,
    string? ShareholderIdNumber,
    string? CompanyName,
    long? AmountCents,
    string? CertificateNumber,
    string? MastersReferenceNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt,
    bool CanSubmit,
    /// <summary>
    /// True when the caller is the claim's creator. False means they reached this claim
    /// as the second party on a Deceased Estate claim (see ClaimAccess.CanView) - the
    /// frontend uses this to show the executor their own identity-verification step
    /// instead of the owner's, and to hide owner-only actions like inviting the executor.
    /// </summary>
    bool IsOwner,
    IReadOnlyList<StepResponse> Steps,
    IReadOnlyList<DocumentResponse> Documents);

public sealed record StepResponse(
    VerificationStepName Name,
    string Label,
    VerificationStepStatus Status,
    string? Detail,
    double? ConfidenceScore);

public sealed record DocumentResponse(
    string DocumentId,
    Domain.Documents.DocumentType DocumentType,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    string? RejectionReason);

public static class ClaimContractMapping
{
    public static ClaimSummaryResponse ToSummary(this Claim claim) => new(
        claim.ClaimId, claim.ClaimType, claim.Status, claim.CompanyName, claim.AmountCents,
        claim.CreatedAt, claim.UpdatedAt);

    public static ClaimDetailResponse ToDetail(
        this Claim claim, IReadOnlyList<VerificationStep> steps, IReadOnlyList<Domain.Documents.StoredDocument> documents,
        string viewerUserId)
    {
        var byName = steps.ToDictionary(s => s.Name, s => s.Status);

        var stepResponses = ClaimWorkflow.RequiredSteps(claim.ClaimType)
            .Select(name =>
            {
                var step = steps.FirstOrDefault(s => s.Name == name);
                return new StepResponse(
                    name,
                    ClaimWorkflow.Label(name),
                    step?.Status ?? VerificationStepStatus.NotStarted,
                    step?.Detail,
                    step?.ConfidenceScore);
            })
            .ToList();

        var documentResponses = documents
            .Select(d => new DocumentResponse(d.DocumentId, d.DocumentType, d.ContentType, d.SizeBytes, d.UploadedAt, d.RejectionReason))
            .ToList();

        return new ClaimDetailResponse(
            claim.ClaimId, claim.ClaimType, claim.Status,
            claim.ShareholderFullName, claim.ShareholderIdNumber, claim.CompanyName,
            claim.AmountCents, claim.CertificateNumber, claim.MastersReferenceNumber,
            claim.CreatedAt, claim.UpdatedAt, claim.SubmittedAt,
            ClaimWorkflow.CanSubmit(claim.ClaimType, byName),
            claim.OwnerUserId == viewerUserId,
            stepResponses, documentResponses);
    }
}
