namespace GuidedMentor.SharedKernel;

/// <summary>
/// Marker interface for Super Admin commands that require enhanced audit logging.
/// In addition to standard audit fields, admin commands log: adminId, target user, and reason.
/// </summary>
public interface IAdminCommand : IAuditableCommand
{
    /// <summary>
    /// The ID of the admin performing the action.
    /// </summary>
    Guid AdminId { get; }

    /// <summary>
    /// The target resource or user this action is directed at.
    /// </summary>
    string AuditTarget { get; }

    /// <summary>
    /// The reason the admin is performing this action (required for all admin operations).
    /// </summary>
    string AuditReason { get; }
}
