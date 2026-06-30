using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Represents an opportunity posting (job, workshop, event, or training) created by a mentor.
/// Mentors can post opportunities from any company or organisation.
/// Each mentor is limited to 5 active postings at any time (all types combined).
/// </summary>
public sealed class OpportunityPosting : AggregateRoot<OpportunityPostingId>
{
    /// <summary>The mentor who created this posting.</summary>
    public MentorId PostedByMentorId { get; private set; }

    /// <summary>Title of the opportunity (5-100 characters).</summary>
    public string Title { get; private set; }

    /// <summary>Type of opportunity: Job, Workshop, Event, or Training.</summary>
    public OpportunityType Type { get; private set; }

    /// <summary>Company or organisation name (2-100 characters, not restricted to mentor's own company).</summary>
    public string OrganisationName { get; private set; }

    /// <summary>Description of the opportunity (100-2000 characters).</summary>
    public string Description { get; private set; }

    /// <summary>Location: city name, "Remote", or "Online".</summary>
    public string Location { get; private set; }

    /// <summary>Date and time for workshops/events/training. Optional for jobs.</summary>
    public DateTime? EventDateTime { get; private set; }

    /// <summary>Employment type (required for jobs only).</summary>
    public EmploymentType? EmploymentType { get; private set; }

    /// <summary>Required AWS skills (0-10 items from predefined list).</summary>
    public IReadOnlyList<string> RequiredSkills { get; private set; }

    /// <summary>Required experience level.</summary>
    public ExperienceLevel RequiredExperience { get; private set; }

    /// <summary>External HTTPS URL for application or registration.</summary>
    public string ExternalUrl { get; private set; }

    /// <summary>When the opportunity was published.</summary>
    public DateTime PublishedAt { get; private set; }

    /// <summary>When the opportunity expires. Computed as min(PublishedAt + 30 days, EventDateTime).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Current status of the posting.</summary>
    public PostingStatus Status { get; private set; }

    /// <summary>Whether the posting has passed its expiry date.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>Whether the posting is active (not expired and status is Active).</summary>
    public bool IsActive => !IsExpired && Status == PostingStatus.Active;

    private OpportunityPosting()
    {
        PostedByMentorId = null!;
        Title = string.Empty;
        OrganisationName = string.Empty;
        Description = string.Empty;
        Location = string.Empty;
        RequiredSkills = [];
        ExternalUrl = string.Empty;
    }

    /// <summary>
    /// Creates a new OpportunityPosting with computed expiry.
    /// Expiry is 30 days from publish date, or the event date, whichever comes first.
    /// </summary>
    public static OpportunityPosting Create(
        MentorId mentorId,
        string title,
        OpportunityType type,
        string organisationName,
        string description,
        string location,
        DateTime? eventDateTime,
        EmploymentType? employmentType,
        IReadOnlyList<string> requiredSkills,
        ExperienceLevel requiredExperience,
        string externalUrl,
        DateTime? publishedAt = null)
    {
        var now = publishedAt ?? DateTime.UtcNow;
        var thirtyDaysExpiry = now.AddDays(30);

        // Expiry: 30 days or event date, whichever comes first
        var expiresAt = eventDateTime.HasValue && eventDateTime.Value < thirtyDaysExpiry
            ? eventDateTime.Value
            : thirtyDaysExpiry;

        var posting = new OpportunityPosting
        {
            Id = OpportunityPostingId.New(),
            PostedByMentorId = mentorId,
            Title = title,
            Type = type,
            OrganisationName = organisationName,
            Description = description,
            Location = location,
            EventDateTime = eventDateTime,
            EmploymentType = employmentType,
            RequiredSkills = requiredSkills,
            RequiredExperience = requiredExperience,
            ExternalUrl = externalUrl,
            PublishedAt = now,
            ExpiresAt = expiresAt,
            Status = PostingStatus.Active
        };

        posting.RaiseDomainEvent(new OpportunityPublishedEvent(
            posting.Id,
            mentorId,
            type,
            requiredSkills,
            now));

        return posting;
    }

    /// <summary>
    /// Reconstitutes an OpportunityPosting from persisted data.
    /// </summary>
    public static OpportunityPosting Reconstitute(
        OpportunityPostingId id,
        MentorId mentorId,
        string title,
        OpportunityType type,
        string organisationName,
        string description,
        string location,
        DateTime? eventDateTime,
        EmploymentType? employmentType,
        IReadOnlyList<string> requiredSkills,
        ExperienceLevel requiredExperience,
        string externalUrl,
        DateTime publishedAt,
        DateTime expiresAt,
        PostingStatus status)
    {
        return new OpportunityPosting
        {
            Id = id,
            PostedByMentorId = mentorId,
            Title = title,
            Type = type,
            OrganisationName = organisationName,
            Description = description,
            Location = location,
            EventDateTime = eventDateTime,
            EmploymentType = employmentType,
            RequiredSkills = requiredSkills,
            RequiredExperience = requiredExperience,
            ExternalUrl = externalUrl,
            PublishedAt = publishedAt,
            ExpiresAt = expiresAt,
            Status = status
        };
    }

    /// <summary>
    /// Renews the opportunity posting by extending expiry by 30 days. Only jobs can be renewed.
    /// </summary>
    public Result Renew()
    {
        if (Type != OpportunityType.Job)
        {
            return Result.Failure("Only job postings can be renewed. Workshops, events, and training auto-archive after their event date.");
        }

        ExpiresAt = DateTime.UtcNow.AddDays(30);
        Status = PostingStatus.Active;
        return Result.Success();
    }

    /// <summary>
    /// Archives the opportunity posting, removing it from public visibility.
    /// </summary>
    public Result Archive()
    {
        Status = PostingStatus.Archived;
        return Result.Success();
    }

    /// <summary>
    /// Marks the posting as expired by the system expiry job.
    /// </summary>
    public void MarkExpired()
    {
        Status = PostingStatus.Expired;
    }
}
