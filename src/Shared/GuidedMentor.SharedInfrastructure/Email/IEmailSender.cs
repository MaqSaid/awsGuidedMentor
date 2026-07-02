namespace GuidedMentor.SharedInfrastructure.Email;

/// <summary>
/// Abstraction for sending email messages. Replaces AWS SES dependency.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
