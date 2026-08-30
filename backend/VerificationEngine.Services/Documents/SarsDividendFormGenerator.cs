using QuestPDF.Fluent;
using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Services.Documents;

/// <summary>
/// The dividend tax declaration a Living Shareholder claim produces once every
/// verification step has passed.
/// </summary>
public sealed class SarsDividendFormGenerator : PdfDocumentBase, IClaimDocumentGenerator
{
    public bool CanGenerate(ClaimType claimType) => claimType == ClaimType.LivingShareholder;

    public byte[] Generate(ClaimDocumentRequest request)
    {
        var claim = request.Claim;
        var amount = claim.AmountCents is { } cents ? $"R {cents / 100m:N2}" : "-";

        return Render("Dividend Tax Declaration", claim.ClaimId, column =>
        {
            SectionHeading(column, "Shareholder Details");
            Field(column, "Full name", claim.ShareholderFullName);
            Field(column, "SA ID number", claim.ShareholderIdNumber);
            Field(column, "Issuing company", claim.CompanyName);

            SectionHeading(column, "Dividend Details");
            Field(column, "Amount claimed", amount);
            Field(column, "Bank account (verified)", request.ExtraFields.GetValueOrDefault("BankAccountLastFour") is { } last4
                ? $"Account ending {last4}" : null);
            Field(column, "Bank", request.ExtraFields.GetValueOrDefault("BankName"));

            SectionHeading(column, "Declaration");
            column.Item().PaddingBottom(8).Text(
                "I declare that the information provided above is true and correct, that I am the " +
                "beneficial owner of the shares described, and that my identity and bank account " +
                "were verified through the process described in this claim's audit trail.")
                .FontSize(9);
        }, request.Signature);
    }
}
