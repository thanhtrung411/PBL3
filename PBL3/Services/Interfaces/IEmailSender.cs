namespace PBL3.Services.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}
