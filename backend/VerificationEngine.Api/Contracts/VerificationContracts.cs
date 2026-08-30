using VerificationEngine.Domain.Documents;

namespace VerificationEngine.Api.Contracts;

public sealed record CreateUploadUrlRequest(DocumentType DocumentType, string ContentType);

public sealed record CreateUploadUrlResponse(string DocumentId, string UploadUrl, DateTimeOffset ExpiresAt);

public sealed record ConfirmUploadRequest(string DocumentId, DocumentType DocumentType, string S3Key, string ContentType, long SizeBytes);

public sealed record FaceCompareRequest(string SelfieDocumentId, string IdDocumentDocumentId);

public sealed record BankLinkStartResponse(string SessionId, string ConsentUrl, IReadOnlyList<string> RequestedScopes);

public sealed record BankLinkCompleteRequest(string SessionId);

public sealed record ExtractDocumentRequest(string DocumentId);

public sealed record ExtractDocumentResponse(IReadOnlyDictionary<string, ExtractedFieldResponse> Fields, string Detail);

public sealed record ExtractedFieldResponse(string Value, double Confidence);

public sealed record ConfirmExtractedFieldsRequest(IReadOnlyDictionary<string, string> ConfirmedFields);

public sealed record SubmitSignatureRequest(string SignerName, string SignatureImageBase64);

public sealed record InviteExecutorRequest(string ExecutorEmail);

public sealed record AcceptInviteRequest(string Token);
