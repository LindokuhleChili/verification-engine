namespace VerificationEngine.Domain.Verification;

/// <summary>
/// Key/value pairs OCR'd out of an uploaded document, with per-field confidence so
/// the UI can highlight what the claimant should double-check. Extraction is never
/// trusted blind — the claimant confirms or corrects every field before submission.
/// </summary>
public sealed record ExtractedFields(IReadOnlyDictionary<string, ExtractedField> Fields, string Detail);

public sealed record ExtractedField(string Value, double Confidence);
