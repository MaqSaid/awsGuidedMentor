using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GuidedMentor.SharedInfrastructure.Email;

/// <summary>
/// Sends emails via Gmail SMTP using MailKit. Replaces AWS SES.
/// Uses App Password for authentication (not OAuth2).
/// </summary>
public sealed class GmailSmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<GmailSmtpEmailSender> _logger;

    public GmailSmtpEmailSender(IOptions<EmailOptions> options, ILogger<GmailSmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogDebug("Email sent to {Recipient} with subject '{Subject}'", to, subject);
    }
}
