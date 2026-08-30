using QuestPDF.Fluent;
using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Services.Documents;

/// <summary>
/// The CM42 share transfer form a Deceased Estate claim produces once the beneficiary,
/// the executor, and the Master's Reference Number have all been verified.
/// </summary>
public sealed class Cm42TransferFormGenerator : PdfDocumentBase, IClaimDocumentGenerator
{
    public bool CanGenerate(ClaimType claimType) => claimType == ClaimType.DeceasedEstate;

    public byte[] Generate(ClaimDocumentRequest request)
    {
        var claim = request.Claim;

        return Render("CM42 - Transfer of Shares (Deceased Estate)", claim.ClaimId, column =>
        {
            SectionHeading(column, "Deceased Shareholder");
            Field(column, "Full name", claim.ShareholderFullName);
            Field(column, "SA ID number", claim.ShareholderIdNumber);
            Field(column, "Issuing company", claim.CompanyName);
            Field(column, "Master's Reference Number", claim.MastersReferenceNumber);

            SectionHeading(column, "Beneficiary");
            Field(column, "Full name", request.ExtraFields.GetValueOrDefault("BeneficiaryName"));
            Field(column, "SA ID number", request.ExtraFields.GetValueOrDefault("BeneficiaryIdNumber"));

            SectionHeading(column, "Executor");
            Field(column, "Full name", request.ExtraFields.GetValueOrDefault("ExecutorName"));
            Field(column, "SA ID number", request.ExtraFields.GetValueOrDefault("ExecutorIdNumber"));
            Field(column, "Letter of Executorship received", request.ExtraFields.GetValueOrDefault("CourierWaybill") is { } wb
                ? $"Yes - waybill {wb}" : "Pending");

            SectionHeading(column, "Declaration");
            column.Item().PaddingBottom(8).Text(
                "The executor named above confirms they are duly appointed under the Master's Reference " +
                "Number stated, and jointly with the beneficiary authorises transfer of the shares described.")
                .FontSize(9);
        }, request.Signature);
    }
}
