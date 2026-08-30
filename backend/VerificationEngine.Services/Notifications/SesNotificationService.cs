using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using VerificationEngine.Services.Configuration;

namespace VerificationEngine.Services.Notifications;

/// <summary>
/// REAL. Amazon SES sends genuine emails. While SES is in sandbox mode (the default for
/// a new account, and never removed here to stay inside the free tier), both the sender
/// and every recipient address must be individually verified in the SES console first.
/// </summary>
public sealed class SesNotificationService : INotificationService
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly EngineOptions _options;

    public SesNotificationService(IAmazonSimpleEmailServiceV2 ses, EngineOptions options)
    {
        _ses = ses;
        _options = options;
    }

    public Task SendExecutorInviteAsync(
        string toEmail, string claimId, string inviteLink, CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            "You've been named executor on a share claim",
            $"<p>You have been named as executor on claim <code>{claimId}</code>.</p>" +
            $"<p>To verify your identity and continue the claim, open this link:</p>" +
            $"<p><a href=\"{inviteLink}\">{inviteLink}</a></p>" +
            "<p>This link expires in 14 days and can only be used once.</p>",
            cancellationToken);

    public Task SendClaimStatusChangedAsync(
        string toEmail, string claimId, string newStatus, string? detail, CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            $"Your claim status changed to {newStatus}",
            $"<p>Claim <code>{claimId}</code> is now <strong>{newStatus}</strong>.</p>" +
            (detail is null ? "" : $"<p>{detail}</p>"),
            cancellationToken);

    public Task SendActionNeededAsync(
        string toEmail, string claimId, string reason, CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            "Action needed on your claim",
            $"<p>Claim <code>{claimId}</code> needs your attention:</p><p>{reason}</p>",
            cancellationToken);

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        await _ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = _options.SenderEmailAddress,
            Destination = new Destination { ToAddresses = [toEmail] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body { Html = new Content { Data = htmlBody, Charset = "UTF-8" } }
                }
            }
        }, cancellationToken);
    }
}
