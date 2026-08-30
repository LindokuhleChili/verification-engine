namespace VerificationEngine.Domain.Persistence;

/// <summary>
/// Every key this application writes to the single DynamoDB table, in one place.
///
/// Single-table design: all entity types share one table and are separated by key
/// prefix rather than by table. Everything belonging to one claim lives in the same
/// partition, so loading a claim with its steps, documents and parties is one Query
/// instead of four round trips.
///
/// <code>
/// PK                  SK                     entity
/// USER#&lt;sub&gt;          PROFILE                claimant profile
/// CLAIM#&lt;claimId&gt;     METADATA               the claim itself
/// CLAIM#&lt;claimId&gt;     STEP#&lt;stepName&gt;        one verification checkpoint
/// CLAIM#&lt;claimId&gt;     DOC#&lt;documentId&gt;       one uploaded file's metadata
/// CLAIM#&lt;claimId&gt;     PARTY#&lt;userId&gt;         second party on a deceased estate
/// INVITE#&lt;token&gt;      METADATA               executor invitation
/// </code>
///
/// GSI1 answers the only access pattern the primary key cannot: "list my claims,
/// newest first" — GSI1PK = USER#&lt;sub&gt;, GSI1SK = CLAIM#&lt;createdAt ISO-8601&gt;.
/// ISO-8601 sorts lexicographically in the same order it sorts chronologically,
/// so no separate sort field is needed.
/// </summary>
public static class TableKeys
{
    public const string PartitionKey = "PK";
    public const string SortKey = "SK";
    public const string Gsi1Name = "GSI1";
    public const string Gsi1PartitionKey = "GSI1PK";
    public const string Gsi1SortKey = "GSI1SK";

    public static string User(string userId) => $"USER#{userId}";
    public static string Claim(string claimId) => $"CLAIM#{claimId}";
    public static string Invite(string token) => $"INVITE#{token}";

    public const string MetadataSort = "METADATA";
    public const string ProfileSort = "PROFILE";

    public static string StepSort(string stepName) => $"STEP#{stepName}";
    public static string DocumentSort(string documentId) => $"DOC#{documentId}";
    public static string PartySort(string userId) => $"PARTY#{userId}";

    /// <summary>Sort key for the "my claims" index. ISO-8601 round-trip format sorts chronologically.</summary>
    public static string ClaimsByUserSort(DateTimeOffset createdAt) =>
        $"CLAIM#{createdAt.UtcDateTime:O}";

    /// <summary>Prefix used to Query only the STEP# items within a claim partition.</summary>
    public const string StepPrefix = "STEP#";
    public const string DocumentPrefix = "DOC#";
    public const string PartyPrefix = "PARTY#";
}
