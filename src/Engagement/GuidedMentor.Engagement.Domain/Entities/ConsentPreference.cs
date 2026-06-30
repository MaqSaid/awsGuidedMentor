using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Represents a user's tracking consent preference.
/// When denied, only auth and error events are tracked.
///
/// Requirements: 30.7, 30.8
/// </summary>
public sealed class ConsentPreference : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Status { get; private set; } = "pending";
    public DateTime UpdatedAt { get; private set; }

    private ConsentPreference() { }

    public static ConsentPreference Create(Guid userId, string status)
    {
        ValidateStatus(status);

        return new ConsentPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateStatus(string newStatus)
    {
        ValidateStatus(newStatus);
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateStatus(string status)
    {
        if (status is not ("granted" or "denied" or "pending"))
            throw new ArgumentException("Status must be 'granted', 'denied', or 'pending'.", nameof(status));
    }
}
