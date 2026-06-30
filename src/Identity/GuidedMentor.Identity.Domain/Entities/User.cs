using GuidedMentor.Identity.Domain.Events;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Entities;

/// <summary>
/// User aggregate root. Manages authentication state, role selection, and onboarding progress.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public Email Email { get; private set; } = null!;
    public Role? ActiveRole { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? ProfilePhotoUrl { get; private set; }
    public AustralianChapter AwsChapter { get; private set; }
    public string City { get; private set; } = string.Empty;
    public OnboardingStatus MentorOnboardingStatus { get; private set; } = OnboardingStatus.NotStarted;
    public OnboardingStatus MenteeOnboardingStatus { get; private set; } = OnboardingStatus.NotStarted;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public bool IsDisabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Computed property: the account is locked if LockedUntil is set and in the future.
    /// </summary>
    public bool IsLocked => LockedUntil is not null && LockedUntil > DateTime.UtcNow;

    /// <summary>
    /// Computed property: the account cannot be used if disabled by an admin.
    /// </summary>
    public bool IsActive => !IsDisabled;

    // Required for ORM/deserialization — no public constructor
    private User() { }

    /// <summary>
    /// Factory method to create a new User aggregate.
    /// </summary>
    public static User Create(
        UserId id,
        Email email,
        string displayName,
        AustralianChapter awsChapter,
        string city,
        string? profilePhotoUrl = null)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            AwsChapter = awsChapter,
            City = city,
            ProfilePhotoUrl = profilePhotoUrl,
            ActiveRole = null,
            MentorOnboardingStatus = OnboardingStatus.NotStarted,
            MenteeOnboardingStatus = OnboardingStatus.NotStarted,
            FailedLoginAttempts = 0,
            LockedUntil = null,
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Sets the initial active role for a user. Fails if a role is already set.
    /// </summary>
    public Result SetInitialRole(Role role)
    {
        if (ActiveRole is not null)
            return Result.Failure("Active role is already set. Use ToggleRole to switch roles.");

        ActiveRole = role;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Switches ActiveRole to the opposite role (Mentor↔Mentee).
    /// Fails if no role has been set yet.
    /// </summary>
    public Result ToggleRole()
    {
        if (ActiveRole is null)
            return Result.Failure("Cannot toggle role. No active role has been set.");

        var previousRole = ActiveRole.Value;
        ActiveRole = previousRole == Role.Mentor ? Role.Mentee : Role.Mentor;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new RoleToggledEvent(Id, previousRole, ActiveRole.Value, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Increments the failed login attempt counter.
    /// Locks the account for 30 minutes if the counter reaches 5.
    /// </summary>
    public void IncrementFailedAttempts()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= MaxFailedAttempts)
        {
            LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            RaiseDomainEvent(new AccountLockedEvent(Id, LockedUntil.Value, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Resets the failed login attempt counter and clears any lockout on successful login.
    /// </summary>
    public void ResetFailedAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the onboarding status for the specified role.
    /// </summary>
    public OnboardingStatus GetOnboardingStatus(Role role)
    {
        return role switch
        {
            Role.Mentor => MentorOnboardingStatus,
            Role.Mentee => MenteeOnboardingStatus,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    /// <summary>
    /// Updates the onboarding status for the given role.
    /// Raises OnboardingCompletedEvent when transitioning to Completed.
    /// </summary>
    public void SetOnboardingStatus(Role role, OnboardingStatus status)
    {
        var previousStatus = GetOnboardingStatus(role);

        switch (role)
        {
            case Role.Mentor:
                MentorOnboardingStatus = status;
                break;
            case Role.Mentee:
                MenteeOnboardingStatus = status;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role));
        }

        UpdatedAt = DateTime.UtcNow;

        if (previousStatus != OnboardingStatus.Completed && status == OnboardingStatus.Completed)
        {
            RaiseDomainEvent(new OnboardingCompletedEvent(Id, role, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Updates the user's AWS chapter. Raises ChapterChangedEvent if the chapter actually changed,
    /// so the Mentoring context can flag compatibility score recalculation.
    /// </summary>
    public void UpdateChapter(AustralianChapter newChapter)
    {
        if (AwsChapter == newChapter)
            return;

        var previousChapter = AwsChapter;
        AwsChapter = newChapter;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ChapterChangedEvent(Id, previousChapter, newChapter, DateTime.UtcNow));
    }

    /// <summary>
    /// Updates basic profile fields (display name, city, profile photo).
    /// </summary>
    public void UpdateProfile(string displayName, string city, string? profilePhotoUrl)
    {
        DisplayName = displayName;
        City = city;
        ProfilePhotoUrl = profilePhotoUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the user account. Called by Super Admin actions.
    /// A disabled account cannot authenticate or use the platform.
    /// </summary>
    public void Disable()
    {
        IsDisabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Re-enables a previously disabled user account.
    /// </summary>
    public void Enable()
    {
        IsDisabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
