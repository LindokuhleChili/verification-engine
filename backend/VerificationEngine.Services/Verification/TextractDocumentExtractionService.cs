using Amazon.Textract;
using Amazon.Textract.Model;
using System.Text.RegularExpressions;
using VerificationEngine.Domain.Verification;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Verification;

/// <summary>
/// REAL. Amazon Textract reads the Letter of Executorship and pulls out the fields a
/// human would otherwise retype — most importantly the Master's Reference Number.
///
/// Nothing extracted here is trusted blind: every field comes back with a confidence
/// score and the claimant confirms or corrects it before the claim is submitted. OCR on
/// a photographed legal document is good, not infallible, and getting an estate
/// reference wrong is expensive to unwind.
/// </summary>
public sealed partial class TextractDocumentExtractionService : IDocumentExtractionService
{
    /// <summary>
    /// Master's Reference Numbers look like <c>012345/2024</c> — a serial, a slash, a year.
    /// Textract's form extraction often mislabels this field, so we also sweep the raw
    /// text for the pattern as a fallback.
    /// </summary>
    [GeneratedRegex(@"\b(\d{3,6}\s*/\s*(19|20)\d{2})\b", RegexOptions.Compiled)]
    private static partial Regex MastersReferenceNumberPattern();

    private readonly IAmazonTextract _textract;
    private readonly EngineOptions _options;

    public TextractDocumentExtractionService(IAmazonTextract textract, EngineOptions options)
    {
        _textract = textract;
        _options = options;
    }

    public async Task<ExtractedFields> ExtractAsync(string s3Key, CancellationToken cancellationToken = default)
    {
        var response = await _textract.AnalyzeDocumentAsync(new AnalyzeDocumentRequest
        {
            Document = new Document
            {
                S3Object = new S3Object { Bucket = _options.DocumentsBucket, Name = s3Key }
            },
            // FORMS gives key/value pairs. We deliberately do not ask for TABLES or
            // QUERIES — both cost more per page and neither helps on this document.
            FeatureTypes = [FeatureType.FORMS]
        }, cancellationToken);

        var blocks = response.Blocks ?? [];
        var byId = blocks.ToDictionary(b => b.Id!);

        var fields = new Dictionary<string, ExtractedField>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyBlock in blocks.Where(IsFormKey))
        {
            var label = ResolveText(keyBlock, byId, RelationshipType.CHILD);
            var valueBlock = ResolveRelated(keyBlock, byId, RelationshipType.VALUE).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(label) || valueBlock is null) continue;

            var value = ResolveText(valueBlock, byId, RelationshipType.CHILD);
            if (string.IsNullOrWhiteSpace(value)) continue;

            // Textract reports confidence for the key and the value separately; the
            // weaker of the two is what the claimant should actually be warned about.
            var confidence = Math.Min(keyBlock.Confidence ?? 0, valueBlock.Confidence ?? 0);

            fields[Normalise(label)] = new ExtractedField(value.Trim(), confidence);
        }

        EnsureMastersReferenceNumber(fields, blocks);

        return new ExtractedFields(
            fields,
            fields.Count == 0
                ? "We could not read any fields from this document. It may be too blurry, or cropped. Please re-upload a clearer scan."
                : $"We read {fields.Count} field(s) from this document. Please check each one before continuing.");
    }

    /// <summary>
    /// Falls back to a raw-text sweep when form extraction missed the reference number,
    /// which happens whenever the Master's Office stamps it rather than printing it in a field.
    /// </summary>
    private static void EnsureMastersReferenceNumber(
        Dictionary<string, ExtractedField> fields, List<Block> blocks)
    {
        if (fields.Keys.Any(k => k.Contains("reference", StringComparison.OrdinalIgnoreCase)))
            return;

        var allText = string.Join(' ', blocks
            .Where(b => b.BlockType == BlockType.LINE)
            .Select(b => b.Text));

        var match = MastersReferenceNumberPattern().Match(allText);
        if (!match.Success) return;

        fields["Masters Reference Number"] = new ExtractedField(
            match.Groups[1].Value.Replace(" ", string.Empty),
            // Lower stated confidence than a real form field — this was pattern-matched
            // out of loose text, so the claimant should look at it closely.
            Confidence: 70);
    }

    private static bool IsFormKey(Block block) =>
        block.BlockType == BlockType.KEY_VALUE_SET &&
        block.EntityTypes?.Contains("KEY") == true;

    private static IEnumerable<Block> ResolveRelated(
        Block block, IReadOnlyDictionary<string, Block> byId, RelationshipType type) =>
        (block.Relationships ?? [])
            .Where(r => r.Type == type)
            .SelectMany(r => r.Ids ?? [])
            .Where(byId.ContainsKey)
            .Select(id => byId[id]);

    private static string ResolveText(
        Block block, IReadOnlyDictionary<string, Block> byId, RelationshipType type) =>
        string.Join(' ', ResolveRelated(block, byId, type)
            .Where(b => b.BlockType == BlockType.WORD)
            .Select(b => b.Text));

    /// <summary>Strips trailing colons and collapses whitespace so "Estate No. :" and "Estate No." are one key.</summary>
    private static string Normalise(string label) =>
        string.Join(' ', label.Replace(":", string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
}
