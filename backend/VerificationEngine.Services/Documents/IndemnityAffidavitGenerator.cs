using QuestPDF.Fluent;
using VerificationEngine.Domain.Claims;

namespace VerificationEngine.Services.Documents;

/// <summary>
/// The legal indemnity affidavit a Lost Certificate claim produces, standing in for the
/// paper affidavit a claimant would otherwise have stamped at a police station.
/// </summary>
public sealed class IndemnityAffidavitGenerator : PdfDocumentBase, IClaimDocumentGenerator
{
    public bool CanGenerate(ClaimType claimType) => claimType == ClaimType.LostCertificate;

    public byte[] Generate(ClaimDocumentRequest request)
    {
        var claim = request.Claim;

        return Render("Indemnity Affidavit - Lost Share Certificate", claim.ClaimId, column =>
        {
            SectionHeading(column, "Shareholder Details");
            Field(column, "Full name", claim.ShareholderFullName);
            Field(column, "SA ID number", claim.ShareholderIdNumber);
            Field(column, "Issuing company", claim.CompanyName);
            Field(column, "Certificate number", claim.CertificateNumber);

            SectionHeading(column, "Affidavit");
            column.Item().PaddingBottom(8).Text(
                $"I, {claim.ShareholderFullName ?? "the shareholder named above"}, declare under oath that the " +
                $"share certificate numbered {claim.CertificateNumber ?? "described above"} has been lost, " +
                "misplaced, or destroyed, was not pledged, sold, or otherwise disposed of, and that I remain the " +
                "lawful owner of the shares it represents. I request that the certificate be dematerialised and " +
                "indemnify the company against any claim arising from a duplicate issue.")
                .FontSize(9);

            SectionHeading(column, "QR Verification");
            column.Item().PaddingBottom(8).Text(
                "A QR code linking this digital affidavit to its claim record is issued alongside this " +
                "document, for the print-and-stamp fallback path where a physical copy still needs a " +
                "commissioner of oaths signature.")
                .FontSize(9).Italic();
        }, request.Signature);
    }
}
