namespace PBL3.Services.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
        CancellationToken cancellationToken = default);
}

public sealed record EmailInlineImage(
    string ContentId,
    string ContentType,
    byte[] Content);
