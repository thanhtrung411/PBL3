using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value.Smtp;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping email to {ToEmail}.", toEmail);
            return;
        }

        var fromEmail = string.IsNullOrWhiteSpace(_options.FromEmail)
            ? _options.Username
            : _options.FromEmail;
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("SMTP email configuration is incomplete. Skipping email to {ToEmail}.", toEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _options.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(toEmail));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));

        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        foreach (var inlineImage in inlineImages ?? Array.Empty<EmailInlineImage>())
        {
            var stream = new MemoryStream(inlineImage.Content);
            var resource = new LinkedResource(stream, inlineImage.ContentType)
            {
                ContentId = inlineImage.ContentId,
                TransferEncoding = System.Net.Mime.TransferEncoding.Base64
            };
            htmlView.LinkedResources.Add(resource);
        }

        message.AlternateViews.Add(htmlView);

#pragma warning disable SYSLIB0014
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };
#pragma warning restore SYSLIB0014

        await client.SendMailAsync(message, cancellationToken);
    }
}
