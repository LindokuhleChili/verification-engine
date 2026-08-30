using Microsoft.AspNetCore.Mvc;
using VerificationEngine.Api.Contracts;
using VerificationEngine.Api.Security;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Domain.Documents;
using VerificationEngine.Services.Documents;
using VerificationEngine.Services.Persistence;
using VerificationEngine.Services.Storage;
using VerificationEngine.Services.Vendors;
using VerificationEngine.Services.Verification;

namespace VerificationEngine.Api.Endpoints;

/// <summary>
/// Document upload plumbing and every verification checkpoint: biometric face match,
/// bank link, OCR extraction, courier tracking, and the final signature that generates
/// the claim's PDF.
/// </summary>
public static class VerificationEndpoints
{
    public static void MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/claims/{claimId}").RequireAuthorization();

        group.MapPost("/documents/upload-url", CreateUploadUrl);
        group.MapPost("/documents/confirm", ConfirmUpload);

        group.MapPost("/verification/face-compare", CompareFaces);
        group.MapPost("/verification/bank-link/start", StartBankLink);
        group.MapPost("/verification/bank-link/complete", CompleteBankLink);
        group.MapPost("/verification/extract", ExtractDocument);
        group.MapPost("/verification/extract/confirm", ConfirmExtractedFields);
        group.MapPost("/verification/signature", SubmitSignature);

        group.MapPost("/courier/book", BookCourier);
        group.MapGet("/courier/track", TrackCourier);
        group.MapPost("/courier/confirm-delivered", ConfirmCourierDelivered);
    }

    private static async Task<IResult> CreateUploadUrl(
        HttpContext http, string claimId, CreateUploadUrlRequest request,
        IClaimRepository repository, IDocumentStore documentStore)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (!await CanActOnClaim(claim, http, repository)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var upload = await documentStore.CreateUploadUrlAsync(claimId, request.DocumentType, request.ContentType);
        return Results.Ok(new CreateUploadUrlResponse(upload.DocumentId, upload.UploadUrl, upload.ExpiresAt));
    }

    private static async Task<IResult> ConfirmUpload(
        HttpContext http, string claimId, ConfirmUploadRequest request,
        IClaimRepository repository, IDocumentStore documentStore)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (!await CanActOnClaim(claim, http, repository)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        if (!await documentStore.ExistsAsync(request.S3Key))
            return Results.BadRequest(new { error = "The upload has not finished landing in storage yet. Please try again in a moment." });

        await repository.SaveDocumentAsync(new StoredDocument
        {
            ClaimId = claimId,
            DocumentId = request.DocumentId,
            DocumentType = request.DocumentType,
            S3Key = request.S3Key,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            UploadedByUserId = CurrentUser.Id(http)
        });

        return Results.NoContent();
    }

    private static async Task<IResult> CompareFaces(
        HttpContext http, string claimId, FaceCompareRequest request,
        IClaimRepository repository, IFaceVerificationService faceService)
    {
        var aggregate = await repository.GetClaimAggregateAsync(claimId);
        if (aggregate is null) return Results.NotFound();
        if (!ClaimAccess.CanView(aggregate.Claim, aggregate.Steps, CurrentUser.Id(http))) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var selfie = aggregate.Documents.FirstOrDefault(d => d.DocumentId == request.SelfieDocumentId);
        var idDoc = aggregate.Documents.FirstOrDefault(d => d.DocumentId == request.IdDocumentDocumentId);
        if (selfie is null || idDoc is null)
            return Results.BadRequest(new { error = "Both the selfie and the ID document must be uploaded before comparing them." });

        var result = await faceService.CompareAsync(selfie.S3Key, idDoc.S3Key);

        // For a deceased-estate claim the executor's identity is a separate step from the
        // shareholder's, distinguished only by who is calling - PartyUserId records that.
        var isExecutorParty = aggregate.Claim.ClaimType == ClaimType.DeceasedEstate
                               && aggregate.Claim.OwnerUserId != CurrentUser.Id(http);

        var stepName = isExecutorParty ? VerificationStepName.ExecutorIdentity : VerificationStepName.IdentityBiometric;

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = stepName,
            Status = result.IsMatch ? VerificationStepStatus.Passed : VerificationStepStatus.Failed,
            Detail = result.Detail,
            ConfidenceScore = result.Similarity,
            PartyUserId = isExecutorParty ? CurrentUser.Id(http) : null
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> StartBankLink(
        HttpContext http, string claimId, IClaimRepository repository, IStitchClient stitch)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var session = await stitch.StartLinkAsync(claimId);

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.BankAccount,
            Status = VerificationStepStatus.InProgress,
            Detail = MockNotice.Wrap("Waiting for the claimant to approve the account-linking consent screen.")
        });

        return Results.Ok(new BankLinkStartResponse(session.SessionId, session.ConsentUrl, session.RequestedScopes));
    }

    private static async Task<IResult> CompleteBankLink(
        HttpContext http, string claimId, BankLinkCompleteRequest request,
        IClaimRepository repository, IStitchClient stitch)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var expectedName = claim.ShareholderFullName ?? "Unknown";
        var result = await stitch.CompleteLinkAsync(request.SessionId, expectedName);

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.BankAccount,
            Status = result.IsVerified ? VerificationStepStatus.Passed : VerificationStepStatus.Failed,
            Detail = result.Detail
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> ExtractDocument(
        HttpContext http, string claimId, ExtractDocumentRequest request,
        IClaimRepository repository, IDocumentExtractionService extraction)
    {
        var aggregate = await repository.GetClaimAggregateAsync(claimId);
        if (aggregate is null) return Results.NotFound();
        if (!ClaimAccess.CanView(aggregate.Claim, aggregate.Steps, CurrentUser.Id(http))) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var document = aggregate.Documents.FirstOrDefault(d => d.DocumentId == request.DocumentId);
        if (document is null) return Results.BadRequest(new { error = "That document has not been uploaded to this claim." });

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.DocumentExtraction,
            Status = VerificationStepStatus.InProgress,
            Detail = "Reading the document. Please review the extracted fields once they appear."
        });

        var result = await extraction.ExtractAsync(document.S3Key);

        var fields = result.Fields.ToDictionary(
            kv => kv.Key,
            kv => new ExtractedFieldResponse(kv.Value.Value, kv.Value.Confidence));

        return Results.Ok(new ExtractDocumentResponse(fields, result.Detail));
    }

    private static async Task<IResult> ConfirmExtractedFields(
        HttpContext http, string claimId, ConfirmExtractedFieldsRequest request, IClaimRepository repository)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        // The claimant has now reviewed every field, so what they confirm becomes the
        // record of truth even if it differs from what OCR originally read.
        if (request.ConfirmedFields.TryGetValue("Masters Reference Number", out var mrn))
            claim.MastersReferenceNumber = mrn;

        await repository.SaveClaimAsync(claim);

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.DocumentExtraction,
            Status = VerificationStepStatus.Passed,
            Detail = "Extracted fields confirmed by claimant."
        });

        return Results.NoContent();
    }

    private static async Task<IResult> SubmitSignature(
        HttpContext http, string claimId, SubmitSignatureRequest request,
        IClaimRepository repository, IDocumentStore documentStore, ClaimDocumentGeneratorFactory generators)
    {
        var aggregate = await repository.GetClaimAggregateAsync(claimId);
        if (aggregate is null) return Results.NotFound();
        if (aggregate.Claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        // Signing generates the claim's final legal document, so every other required
        // step must already have passed - otherwise a claimant could sign (and get a
        // completed PDF) before their identity, bank link, or the executor/courier leg
        // of a Deceased Estate claim ever succeeded.
        var passedByOthers = aggregate.Steps
            .Where(s => s.Name != VerificationStepName.Signature && s.Status == VerificationStepStatus.Passed)
            .Select(s => s.Name)
            .ToHashSet();
        var outstanding = ClaimWorkflow.RequiredSteps(aggregate.Claim.ClaimType)
            .Where(s => s != VerificationStepName.Signature && !passedByOthers.Contains(s))
            .ToList();
        if (outstanding.Count > 0)
            return Results.BadRequest(new
            {
                error = "Every other verification step must pass before you can sign.",
                outstanding = outstanding.Select(ClaimWorkflow.Label)
            });

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(request.SignatureImageBase64);
        }
        catch (FormatException)
        {
            return Results.BadRequest(new { error = "The signature image was not valid base64 image data." });
        }

        // See SignatureRecord: this is a hashed, timestamped audit trail, not an
        // accredited Advanced Electronic Signature.
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(signatureBytes));
        var signature = new SignatureRecord(request.SignerName, hash, DateTimeOffset.UtcNow);

        var signatureKey = $"signatures/{claimId}/{Guid.NewGuid():N}.png";
        await documentStore.PutBytesAsync(signatureKey, signatureBytes, "image/png");

        var extraFields = BuildExtraFieldsForDocument(aggregate);
        var pdfBytes = generators.For(aggregate.Claim.ClaimType).Generate(
            new ClaimDocumentRequest(aggregate.Claim, signature, extraFields));

        var documentKey = $"generated/{claimId}/{aggregate.Claim.ClaimType}.pdf";
        await documentStore.PutBytesAsync(documentKey, pdfBytes, "application/pdf");

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.Signature,
            Status = VerificationStepStatus.Passed,
            Detail = $"Signed by {request.SignerName}."
        });

        aggregate.Claim.GeneratedDocumentKey = documentKey;
        aggregate.Claim.Status = ClaimStatus.Complete;
        await repository.SaveClaimAsync(aggregate.Claim);

        return Results.Ok(new { documentKey, signatureHash = hash });
    }

    private static async Task<IResult> BookCourier(
        HttpContext http, string claimId, IClaimRepository repository, ICourierClient courier)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var booking = await courier.BookCollectionAsync(claimId, "Address on file");

        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.CourierDelivery,
            Status = VerificationStepStatus.AwaitingCounterparty,
            Detail = MockNotice.Wrap($"Collection booked, waybill {booking.WaybillNumber}, estimated {booking.EstimatedCollectionDate:yyyy-MM-dd}.")
        });

        return Results.Ok(booking);
    }

    private static async Task<IResult> ConfirmCourierDelivered(HttpContext http, string claimId, IClaimRepository repository)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        // In production this would be a courier webhook, not a claimant click. Simulated
        // here because there is no real courier account to receive a real webhook from -
        // see MockCourierClient.
        await repository.SaveStepAsync(new VerificationStep
        {
            ClaimId = claimId,
            Name = VerificationStepName.CourierDelivery,
            Status = VerificationStepStatus.Passed,
            Detail = MockNotice.Wrap("Original Letter of Executorship confirmed received by the processing team.")
        });

        return Results.NoContent();
    }

    private static async Task<IResult> TrackCourier(
        HttpContext http, string claimId, [FromQuery] string waybill,
        IClaimRepository repository, ICourierClient courier)
    {
        var claim = await repository.GetClaimAsync(claimId);
        if (claim is null) return Results.NotFound();
        if (claim.OwnerUserId != CurrentUser.Id(http)) return Results.StatusCode(StatusCodes.Status403Forbidden);

        return Results.Ok(await courier.TrackAsync(waybill));
    }

    /// <summary>Owner may always act; on a deceased-estate claim, a verified executor party may too.</summary>
    private static async Task<bool> CanActOnClaim(Claim claim, HttpContext http, IClaimRepository repository)
    {
        var userId = CurrentUser.Id(http);
        if (claim.OwnerUserId == userId) return true;

        var aggregate = await repository.GetClaimAggregateAsync(claim.ClaimId);
        return aggregate is not null && aggregate.Steps.Any(s => s.PartyUserId == userId);
    }

    private static Dictionary<string, string> BuildExtraFieldsForDocument(ClaimAggregate aggregate)
    {
        var fields = new Dictionary<string, string>();

        var bankStep = aggregate.Steps.FirstOrDefault(s => s.Name == VerificationStepName.BankAccount);
        if (bankStep?.Detail is { } detail) fields["BankName"] = "Standard Bank of South Africa";

        var courierStep = aggregate.Steps.FirstOrDefault(s => s.Name == VerificationStepName.CourierDelivery);
        if (courierStep is not null) fields["CourierWaybill"] = ExtractWaybill(courierStep.Detail);

        return fields;
    }

    private static string ExtractWaybill(string? detail)
    {
        if (detail is null) return string.Empty;
        var marker = "waybill ";
        var index = detail.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return string.Empty;
        var start = index + marker.Length;
        var end = detail.IndexOf(',', start);
        return end < 0 ? detail[start..].TrimEnd('.') : detail[start..end];
    }
}
