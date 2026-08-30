namespace VerificationEngine.Services.Notifications;

/// <summary>
/// All claimant- and executor-facing email. SES replaces the WhatsApp notifications in
/// the original scenario document, per the project brief - WhatsApp Business API is out
/// of scope for this build.
/// </summary>
public interface INotificationService
{
    Task SendExecutorInviteAsync(string toEmail, string claimId, string inviteLink, CancellationToken cancellationToken = default);

    Task SendClaimStatusChangedAsync(string toEmail, string claimId, string newStatus, string? detail, CancellationToken cancellationToken = default);

    Task SendActionNeededAsync(string toEmail, string claimId, string reason, CancellationToken cancellationToken = default);
}
