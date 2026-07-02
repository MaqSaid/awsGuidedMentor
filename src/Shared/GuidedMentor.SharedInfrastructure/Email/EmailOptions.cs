namespace GuidedMentor.SharedInfrastructure.Email;

/// <summary>
/// Configuration options for Gmail SMTP email sending.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "GuidedMentor";
}
