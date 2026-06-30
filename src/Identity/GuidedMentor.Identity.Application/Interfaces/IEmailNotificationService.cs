namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Abstraction for sending email notifications (lockout, verification, etc.).
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Sends an account lockout notification with recovery instructions.
    /// </summary>
    Task SendAccountLockedNotificationAsync(string email, DateTime lockedUntil, CancellationToken ct);
}
