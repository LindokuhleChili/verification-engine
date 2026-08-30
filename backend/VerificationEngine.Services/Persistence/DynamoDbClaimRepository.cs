using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using VerificationEngine.Domain.Claims;
using VerificationEngine.Domain.Documents;
using VerificationEngine.Domain.Persistence;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Persistence;

/// <summary>
/// Single-table implementation. Item shapes are mapped by hand rather than with the
/// object-persistence model: the table holds several unrelated entity types, and an
/// explicit mapping makes the stored shape obvious when reading the code.
/// </summary>
public sealed class DynamoDbClaimRepository : IClaimRepository
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly EngineOptions _options;

    public DynamoDbClaimRepository(IAmazonDynamoDB dynamo, EngineOptions options)
    {
        _dynamo = dynamo;
        _options = options;
    }

    public async Task SaveClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        var item = new Dictionary<string, AttributeValue>
        {
            [TableKeys.PartitionKey] = S(TableKeys.Claim(claim.ClaimId)),
            [TableKeys.SortKey] = S(TableKeys.MetadataSort),

            // Projected onto GSI1 so "list my claims, newest first" is one Query.
            [TableKeys.Gsi1PartitionKey] = S(TableKeys.User(claim.OwnerUserId)),
            [TableKeys.Gsi1SortKey] = S(TableKeys.ClaimsByUserSort(claim.CreatedAt)),

            ["EntityType"] = S("Claim"),
            ["ClaimId"] = S(claim.ClaimId),
            ["OwnerUserId"] = S(claim.OwnerUserId),
            ["OwnerEmail"] = S(claim.OwnerEmail),
            ["ClaimType"] = S(claim.ClaimType.ToString()),
            ["Status"] = S(claim.Status.ToString()),
            ["CreatedAt"] = S(Iso(claim.CreatedAt)),
            ["UpdatedAt"] = S(Iso(claim.UpdatedAt))
        };

        PutOptional(item, "ShareholderIdNumber", claim.ShareholderIdNumber);
        PutOptional(item, "ShareholderFullName", claim.ShareholderFullName);
        PutOptional(item, "CompanyName", claim.CompanyName);
        PutOptional(item, "CertificateNumber", claim.CertificateNumber);
        PutOptional(item, "MastersReferenceNumber", claim.MastersReferenceNumber);
        PutOptional(item, "GeneratedDocumentKey", claim.GeneratedDocumentKey);

        if (claim.AmountCents is { } cents)
            item["AmountCents"] = new AttributeValue { N = cents.ToString() };

        if (claim.SubmittedAt is { } submitted)
            item["SubmittedAt"] = S(Iso(submitted));

        await _dynamo.PutItemAsync(new PutItemRequest(_options.TableName, item), cancellationToken);
    }

    public async Task<Claim?> GetClaimAsync(string claimId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = ClaimMetadataKey(claimId)
        }, cancellationToken);

        return response.IsItemSet ? ToClaim(response.Item) : null;
    }

    public async Task<IReadOnlyList<Claim>> ListClaimsForUserAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName = _options.TableName,
            IndexName = TableKeys.Gsi1Name,
            KeyConditionExpression = "#pk = :pk",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#pk"] = TableKeys.Gsi1PartitionKey },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pk"] = S(TableKeys.User(userId)) },

            // Descending on an ISO-8601 sort key gives newest first without a second attribute.
            ScanIndexForward = false
        }, cancellationToken);

        return response.Items.Select(ToClaim).ToList();
    }

    public async Task<ClaimAggregate?> GetClaimAggregateAsync(
        string claimId, CancellationToken cancellationToken = default)
    {
        // One Query pulls the whole partition - claim, steps and documents together -
        // which is the entire reason for the single-table design.
        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName = _options.TableName,
            KeyConditionExpression = "#pk = :pk",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#pk"] = TableKeys.PartitionKey },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pk"] = S(TableKeys.Claim(claimId)) }
        }, cancellationToken);

        var metadata = response.Items.FirstOrDefault(i => Str(i, TableKeys.SortKey) == TableKeys.MetadataSort);
        if (metadata is null) return null;

        var steps = response.Items
            .Where(i => HasSortPrefix(i, TableKeys.StepPrefix))
            .Select(ToStep)
            .ToList();

        var documents = response.Items
            .Where(i => HasSortPrefix(i, TableKeys.DocumentPrefix))
            .Select(ToDocument)
            .ToList();

        return new ClaimAggregate(ToClaim(metadata), steps, documents);
    }

    public async Task SaveStepAsync(VerificationStep step, CancellationToken cancellationToken = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [TableKeys.PartitionKey] = S(TableKeys.Claim(step.ClaimId)),
            [TableKeys.SortKey] = S(TableKeys.StepSort(step.Name.ToString())),
            ["EntityType"] = S("VerificationStep"),
            ["ClaimId"] = S(step.ClaimId),
            ["Name"] = S(step.Name.ToString()),
            ["Status"] = S(step.Status.ToString()),
            ["UpdatedAt"] = S(Iso(DateTimeOffset.UtcNow))
        };

        PutOptional(item, "Detail", step.Detail);
        PutOptional(item, "PartyUserId", step.PartyUserId);

        if (step.ConfidenceScore is { } score)
            item["ConfidenceScore"] = new AttributeValue { N = score.ToString("F2") };

        await _dynamo.PutItemAsync(new PutItemRequest(_options.TableName, item), cancellationToken);
    }

    public async Task SaveDocumentAsync(StoredDocument document, CancellationToken cancellationToken = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [TableKeys.PartitionKey] = S(TableKeys.Claim(document.ClaimId)),
            [TableKeys.SortKey] = S(TableKeys.DocumentSort(document.DocumentId)),
            ["EntityType"] = S("StoredDocument"),
            ["ClaimId"] = S(document.ClaimId),
            ["DocumentId"] = S(document.DocumentId),
            ["DocumentType"] = S(document.DocumentType.ToString()),
            ["S3Key"] = S(document.S3Key),
            ["ContentType"] = S(document.ContentType),
            ["SizeBytes"] = new AttributeValue { N = document.SizeBytes.ToString() },
            ["UploadedAt"] = S(Iso(document.UploadedAt))
        };

        PutOptional(item, "UploadedByUserId", document.UploadedByUserId);
        PutOptional(item, "RejectionReason", document.RejectionReason);

        await _dynamo.PutItemAsync(new PutItemRequest(_options.TableName, item), cancellationToken);
    }

    public async Task SaveInviteAsync(ExecutorInvite invite, CancellationToken cancellationToken = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [TableKeys.PartitionKey] = S(TableKeys.Invite(invite.Token)),
            [TableKeys.SortKey] = S(TableKeys.MetadataSort),
            ["EntityType"] = S("ExecutorInvite"),
            ["Token"] = S(invite.Token),
            ["ClaimId"] = S(invite.ClaimId),
            ["InvitedEmail"] = S(invite.InvitedEmail),
            ["CreatedAt"] = S(Iso(invite.CreatedAt)),
            ["ExpiresAt"] = S(Iso(invite.ExpiresAt)),

            // DynamoDB TTL deletes expired invites for us. Free, and it means an abandoned
            // invitation cannot sit redeemable in the table forever.
            ["ExpiresAtEpoch"] = new AttributeValue { N = invite.ExpiresAt.ToUnixTimeSeconds().ToString() }
        };

        PutOptional(item, "AcceptedByUserId", invite.AcceptedByUserId);

        await _dynamo.PutItemAsync(new PutItemRequest(_options.TableName, item), cancellationToken);
    }

    public async Task<ExecutorInvite?> GetInviteAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [TableKeys.PartitionKey] = S(TableKeys.Invite(token)),
                [TableKeys.SortKey] = S(TableKeys.MetadataSort)
            }
        }, cancellationToken);

        if (!response.IsItemSet) return null;

        return new ExecutorInvite
        {
            Token = Str(response.Item, "Token")!,
            ClaimId = Str(response.Item, "ClaimId")!,
            InvitedEmail = Str(response.Item, "InvitedEmail")!,
            AcceptedByUserId = Str(response.Item, "AcceptedByUserId"),
            CreatedAt = Time(response.Item, "CreatedAt") ?? DateTimeOffset.UtcNow,
            ExpiresAt = Time(response.Item, "ExpiresAt") ?? DateTimeOffset.UtcNow
        };
    }

    // ---- mapping helpers -------------------------------------------------

    private static Dictionary<string, AttributeValue> ClaimMetadataKey(string claimId) => new()
    {
        [TableKeys.PartitionKey] = S(TableKeys.Claim(claimId)),
        [TableKeys.SortKey] = S(TableKeys.MetadataSort)
    };

    private static bool HasSortPrefix(Dictionary<string, AttributeValue> item, string prefix) =>
        Str(item, TableKeys.SortKey) is { } sk && sk.StartsWith(prefix, StringComparison.Ordinal);

    private static Claim ToClaim(Dictionary<string, AttributeValue> item) => new()
    {
        ClaimId = Str(item, "ClaimId")!,
        OwnerUserId = Str(item, "OwnerUserId")!,
        OwnerEmail = Str(item, "OwnerEmail") ?? string.Empty,
        ClaimType = Enum.Parse<ClaimType>(Str(item, "ClaimType")!),
        Status = Enum.Parse<ClaimStatus>(Str(item, "Status")!),
        ShareholderIdNumber = Str(item, "ShareholderIdNumber"),
        ShareholderFullName = Str(item, "ShareholderFullName"),
        CompanyName = Str(item, "CompanyName"),
        CertificateNumber = Str(item, "CertificateNumber"),
        MastersReferenceNumber = Str(item, "MastersReferenceNumber"),
        GeneratedDocumentKey = Str(item, "GeneratedDocumentKey"),
        AmountCents = Num(item, "AmountCents") is { } n ? (long)n : null,
        CreatedAt = Time(item, "CreatedAt") ?? DateTimeOffset.UtcNow,
        UpdatedAt = Time(item, "UpdatedAt") ?? DateTimeOffset.UtcNow,
        SubmittedAt = Time(item, "SubmittedAt")
    };

    private static VerificationStep ToStep(Dictionary<string, AttributeValue> item) => new()
    {
        ClaimId = Str(item, "ClaimId")!,
        Name = Enum.Parse<VerificationStepName>(Str(item, "Name")!),
        Status = Enum.Parse<VerificationStepStatus>(Str(item, "Status")!),
        Detail = Str(item, "Detail"),
        PartyUserId = Str(item, "PartyUserId"),
        ConfidenceScore = Num(item, "ConfidenceScore"),
        UpdatedAt = Time(item, "UpdatedAt") ?? DateTimeOffset.UtcNow
    };

    private static StoredDocument ToDocument(Dictionary<string, AttributeValue> item) => new()
    {
        ClaimId = Str(item, "ClaimId")!,
        DocumentId = Str(item, "DocumentId")!,
        DocumentType = Enum.Parse<DocumentType>(Str(item, "DocumentType")!),
        S3Key = Str(item, "S3Key")!,
        ContentType = Str(item, "ContentType") ?? "application/octet-stream",
        SizeBytes = Num(item, "SizeBytes") is { } n ? (long)n : 0,
        UploadedByUserId = Str(item, "UploadedByUserId"),
        RejectionReason = Str(item, "RejectionReason"),
        UploadedAt = Time(item, "UploadedAt") ?? DateTimeOffset.UtcNow
    };

    /// <summary>Round-trip format, always UTC, so stored timestamps sort lexicographically.</summary>
    private static string Iso(DateTimeOffset value) => value.UtcDateTime.ToString("O");

    private static AttributeValue S(string value) => new() { S = value };

    /// <summary>DynamoDB stores no value for null and rejects empty strings in keys, so skip both.</summary>
    private static void PutOptional(Dictionary<string, AttributeValue> item, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value)) item[name] = S(value);
    }

    private static string? Str(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var v) ? v.S : null;

    private static double? Num(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var v) && double.TryParse(v.N, out var parsed) ? parsed : null;

    private static DateTimeOffset? Time(Dictionary<string, AttributeValue> item, string name) =>
        Str(item, name) is { } s && DateTimeOffset.TryParse(s, out var parsed) ? parsed : null;
}
