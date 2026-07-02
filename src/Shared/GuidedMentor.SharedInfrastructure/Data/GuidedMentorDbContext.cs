using GuidedMentor.SharedInfrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.SharedInfrastructure.Data;

/// <summary>
/// EF Core DbContext for GuidedMentor PostgreSQL database.
/// Maps all persistence entities to their corresponding tables.
/// </summary>
public sealed class GuidedMentorDbContext : DbContext
{
    public GuidedMentorDbContext(DbContextOptions<GuidedMentorDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<MentorEntity> Mentors => Set<MentorEntity>();
    public DbSet<MenteeEntity> Mentees => Set<MenteeEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<AuthTokenEntity> AuthTokens => Set<AuthTokenEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<MeetupEntity> Meetups => Set<MeetupEntity>();
    public DbSet<EngagementEventEntity> EngagementEvents => Set<EngagementEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.DisplayName).HasColumnName("display_name").IsRequired();
            e.Property(x => x.ProfilePhotoUrl).HasColumnName("profile_photo_url");
            e.Property(x => x.AwsChapter).HasColumnName("aws_chapter");
            e.Property(x => x.City).HasColumnName("city");
            e.Property(x => x.ActiveRole).HasColumnName("active_role");
            e.Property(x => x.MentorOnboardingStatus).HasColumnName("mentor_onboarding_status");
            e.Property(x => x.MenteeOnboardingStatus).HasColumnName("mentee_onboarding_status");
            e.Property(x => x.IsDisabled).HasColumnName("is_disabled");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<MentorEntity>(e =>
        {
            e.ToTable("mentors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ExpertiseAreas).HasColumnName("expertise_areas");
            e.Property(x => x.Certifications).HasColumnName("certifications");
            e.Property(x => x.Topics).HasColumnName("topics");
            e.Property(x => x.YearsOfExperience).HasColumnName("years_of_experience");
            e.Property(x => x.MaxMentees).HasColumnName("max_mentees");
            e.Property(x => x.ActiveMenteeCount).HasColumnName("active_mentee_count");
            e.Property(x => x.Availability).HasColumnName("availability").HasColumnType("jsonb");
            e.Property(x => x.SessionFormats).HasColumnName("session_formats");
            e.Property(x => x.ProfessionalTitle).HasColumnName("professional_title");
            e.Property(x => x.CompanyName).HasColumnName("company_name");
            e.Property(x => x.Bio).HasColumnName("bio");
            e.Property(x => x.AvailabilityStatus).HasColumnName("availability_status");
            e.Property(x => x.UnavailabilityReason).HasColumnName("unavailability_reason");
            e.Property(x => x.ReturnDate).HasColumnName("return_date");
            e.Property(x => x.UnavailableSince).HasColumnName("unavailable_since");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<MenteeEntity>(e =>
        {
            e.ToTable("mentees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Skills).HasColumnName("skills");
            e.Property(x => x.ExperienceLevel).HasColumnName("experience_level");
            e.Property(x => x.YearsOfExperience).HasColumnName("years_of_experience");
            e.Property(x => x.PrimaryGoal).HasColumnName("primary_goal");
            e.Property(x => x.GoalDescription).HasColumnName("goal_description");
            e.Property(x => x.PreferredDuration).HasColumnName("preferred_duration");
            e.Property(x => x.Availability).HasColumnName("availability").HasColumnType("jsonb");
            e.Property(x => x.CommunicationPreference).HasColumnName("communication_preference");
            e.Property(x => x.ResumeUrl).HasColumnName("resume_url");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SessionEntity>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MenteeId).HasColumnName("mentee_id");
            e.Property(x => x.MentorId).HasColumnName("mentor_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.SessionPlan).HasColumnName("session_plan").HasColumnType("jsonb");
            e.Property(x => x.ChecklistState).HasColumnName("checklist_state").HasColumnType("jsonb");
            e.Property(x => x.MenteeCompletedAt).HasColumnName("mentee_completed_at");
            e.Property(x => x.MentorCompletedAt).HasColumnName("mentor_completed_at");
            e.Property(x => x.LockId).HasColumnName("lock_id");
            e.Property(x => x.LockExpiresAt).HasColumnName("lock_expires_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<NotificationEntity>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.Message).HasColumnName("message");
            e.Property(x => x.ActionUrl).HasColumnName("action_url");
            e.Property(x => x.IsRead).HasColumnName("is_read");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<AuthTokenEntity>(e =>
        {
            e.ToTable("auth_tokens");
            e.HasKey(x => x.Token);
            e.Property(x => x.Token).HasColumnName("token");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Used).HasColumnName("used");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        });

        modelBuilder.Entity<OpportunityEntity>(e =>
        {
            e.ToTable("opportunities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MentorId).HasColumnName("mentor_id");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.OrganisationName).HasColumnName("organisation_name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Location).HasColumnName("location");
            e.Property(x => x.EventDateTime).HasColumnName("event_date_time");
            e.Property(x => x.EmploymentType).HasColumnName("employment_type");
            e.Property(x => x.RequiredSkills).HasColumnName("required_skills");
            e.Property(x => x.RequiredExperience).HasColumnName("required_experience");
            e.Property(x => x.ExternalUrl).HasColumnName("external_url");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.PublishedAt).HasColumnName("published_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        });

        modelBuilder.Entity<MeetupEntity>(e =>
        {
            e.ToTable("meetups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Chapter).HasColumnName("chapter");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.EventDate).HasColumnName("event_date");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.VenueName).HasColumnName("venue_name");
            e.Property(x => x.VenueAddress).HasColumnName("venue_address");
            e.Property(x => x.EventUrl).HasColumnName("event_url");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.IsCancelled).HasColumnName("is_cancelled");
            e.Property(x => x.ConfirmedAttendees).HasColumnName("confirmed_attendees");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<EngagementEventEntity>(e =>
        {
            e.ToTable("engagement_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserIdHash).HasColumnName("user_id_hash");
            e.Property(x => x.EventType).HasColumnName("event_type");
            e.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            e.Property(x => x.ActiveRole).HasColumnName("active_role");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
