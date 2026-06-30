using FluentAssertions;
using GuidedMentor.Tools.SeedData.Fakers;

namespace GuidedMentor.Tools.SeedData.Tests;

public sealed class AustralianFakersTests
{
    [Fact]
    public void MentorFaker_Generates20Profiles()
    {
        var mentors = AustralianFakers.MentorFaker.Generate(20);

        mentors.Should().HaveCount(20);
    }

    [Fact]
    public void MentorFaker_GeneratesProfiles_AcrossAtLeast8Chapters()
    {
        // GenerateFullDataSet ensures 8+ chapter coverage
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var distinctChapters = dataSet.Mentors.Select(m => m.Chapter).Distinct().Count();
        distinctChapters.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void MentorFaker_GeneratesVariedExpertiseAndCertifications()
    {
        var mentors = AustralianFakers.MentorFaker.Generate(20);

        mentors.Should().AllSatisfy(m =>
        {
            m.ExpertiseAreas.Should().NotBeEmpty();
            m.ExpertiseAreas.Count.Should().BeInRange(2, 8);
            m.Certifications.Should().NotBeEmpty();
            m.Certifications.Count.Should().BeInRange(1, 5);
            m.YearsOfExperience.Should().BeInRange(3, 25);
        });
    }

    [Fact]
    public void MentorFaker_GeneratesRealisticAustralianData()
    {
        var mentors = AustralianFakers.MentorFaker.Generate(5);

        mentors.Should().AllSatisfy(m =>
        {
            m.FullName.Should().NotBeNullOrWhiteSpace();
            m.Email.Should().Contain("@");
            m.CompanyName.Should().NotBeNullOrWhiteSpace();
            m.ProfessionalTitle.Should().NotBeNullOrWhiteSpace();
            m.Bio.Should().NotBeNullOrWhiteSpace();
            m.MentorId.Should().NotBeNullOrWhiteSpace();
            m.UserId.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void MenteeFaker_Generates30Profiles()
    {
        var mentees = AustralianFakers.MenteeFaker.Generate(30);

        mentees.Should().HaveCount(30);
    }

    [Fact]
    public void MenteeFaker_GeneratesVariedSkillsAndGoals()
    {
        var mentees = AustralianFakers.MenteeFaker.Generate(30);

        mentees.Should().AllSatisfy(m =>
        {
            m.Skills.Should().NotBeEmpty();
            m.Skills.Count.Should().BeInRange(1, 7);
            m.ExperienceLevel.Should().BeOneOf("beginner", "intermediate", "advanced");
            m.PrimaryGoal.Should().BeOneOf("career_transition", "skill_development", "certification_preparation", "project_guidance");
            m.GoalDescription.Should().NotBeNullOrWhiteSpace();
            m.YearsOfExperience.Should().BeInRange(0, 15);
        });

        // Ensure varied goals (not all the same)
        var distinctGoals = mentees.Select(m => m.PrimaryGoal).Distinct().Count();
        distinctGoals.Should().BeGreaterThan(1);
    }

    [Fact]
    public void MenteeFaker_GeneratesRealisticAustralianData()
    {
        var mentees = AustralianFakers.MenteeFaker.Generate(5);

        mentees.Should().AllSatisfy(m =>
        {
            m.FullName.Should().NotBeNullOrWhiteSpace();
            m.Email.Should().Contain("@");
            m.Chapter.Should().NotBeNullOrWhiteSpace();
            m.City.Should().NotBeNullOrWhiteSpace();
            m.MenteeId.Should().NotBeNullOrWhiteSpace();
            m.UserId.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void JobPostingFaker_GeneratesCorrectCounts()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.JobPostings.Count(j => j.Status == "active").Should().Be(10);
        dataSet.JobPostings.Count(j => j.Status == "expired").Should().Be(3);
        dataSet.JobPostings.Count(j => j.Status == "archived").Should().Be(2);
        dataSet.JobPostings.Should().HaveCount(15);
    }

    [Fact]
    public void JobPostingFaker_PostingsFromDifferentMentors()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var distinctMentorIds = dataSet.JobPostings.Select(j => j.MentorId).Distinct().Count();
        distinctMentorIds.Should().BeGreaterThan(1, "postings should come from different mentors");
    }

    [Fact]
    public void JobPostingFaker_GeneratesValidPostingData()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.JobPostings.Should().AllSatisfy(j =>
        {
            j.PostingId.Should().NotBeNullOrWhiteSpace();
            j.MentorId.Should().NotBeNullOrWhiteSpace();
            j.Title.Should().NotBeNullOrWhiteSpace();
            j.OrganisationName.Should().NotBeNullOrWhiteSpace();
            j.Description.Should().NotBeNullOrWhiteSpace();
            j.Location.Should().NotBeNullOrWhiteSpace();
            j.RequiredSkills.Should().NotBeEmpty();
            j.ExternalUrl.Should().NotBeNullOrWhiteSpace();
            j.OpportunityType.Should().BeOneOf("job", "workshop", "event", "training");
            j.Status.Should().BeOneOf("active", "expired", "archived");
        });
    }

    [Fact]
    public void MeetupEventFaker_GeneratesCorrectCounts()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var upcoming = dataSet.MeetupEvents.Count(e => e.EventDate > DateTime.UtcNow);
        var past = dataSet.MeetupEvents.Count(e => e.EventDate <= DateTime.UtcNow);

        upcoming.Should().Be(5);
        past.Should().Be(2);
        dataSet.MeetupEvents.Should().HaveCount(7);
    }

    [Fact]
    public void MeetupEventFaker_UpcomingEventsAcross5Chapters()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var upcomingChapters = dataSet.MeetupEvents
            .Where(e => e.EventDate > DateTime.UtcNow)
            .Select(e => e.Chapter)
            .Distinct()
            .Count();

        upcomingChapters.Should().Be(5);
    }

    [Fact]
    public void MeetupEventFaker_UpcomingEventsHaveAttendees()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var upcomingEvents = dataSet.MeetupEvents.Where(e => e.EventDate > DateTime.UtcNow);

        upcomingEvents.Should().AllSatisfy(e =>
        {
            e.ConfirmedAttendees.Should().NotBeEmpty("upcoming events should have confirmed attendees");
        });
    }

    [Fact]
    public void MeetupEventFaker_GeneratesValidEventData()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.MeetupEvents.Should().AllSatisfy(e =>
        {
            e.MeetupEventId.Should().NotBeNullOrWhiteSpace();
            e.Chapter.Should().NotBeNullOrWhiteSpace();
            e.Title.Should().NotBeNullOrWhiteSpace();
            e.VenueName.Should().NotBeNullOrWhiteSpace();
            e.VenueAddress.Should().NotBeNullOrWhiteSpace();
            e.StartTime.Should().MatchRegex(@"^\d{2}:\d{2}$");
            e.EndTime.Should().MatchRegex(@"^\d{2}:\d{2}$");
            e.CreatedBy.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void NotificationFaker_Generates50Records()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.Notifications.Should().HaveCount(50);
    }

    [Fact]
    public void NotificationFaker_CoversAllNotificationTypes()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var types = dataSet.Notifications.Select(n => n.Type).Distinct().ToList();

        // Should cover multiple notification types
        types.Should().Contain("request_sent");
        types.Count.Should().BeGreaterThan(3, "should have varied notification types");
    }

    [Fact]
    public void NotificationFaker_HasVariedReadUnreadStatus()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var readCount = dataSet.Notifications.Count(n => n.IsRead);
        var unreadCount = dataSet.Notifications.Count(n => !n.IsRead);

        readCount.Should().BeGreaterThan(0, "should have some read notifications");
        unreadCount.Should().BeGreaterThan(0, "should have some unread notifications");
    }

    [Fact]
    public void NotificationFaker_DistributedAcrossUsers()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var distinctRecipients = dataSet.Notifications.Select(n => n.RecipientUserId).Distinct().Count();
        distinctRecipients.Should().BeGreaterThan(5, "notifications should be distributed across many users");
    }

    [Fact]
    public void NotificationFaker_GeneratesValidNotificationData()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.Notifications.Should().AllSatisfy(n =>
        {
            n.NotificationId.Should().NotBeNullOrWhiteSpace();
            n.RecipientUserId.Should().NotBeNullOrWhiteSpace();
            n.Type.Should().NotBeNullOrWhiteSpace();
            n.Message.Should().NotBeNullOrWhiteSpace();
            n.ActionUrl.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GenerateFullDataSet_IncludesUnavailableMentor()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        var unavailable = dataSet.Mentors.Where(m => m.AvailabilityStatus == "Unavailable").ToList();
        unavailable.Should().HaveCount(1);
        unavailable[0].UnavailabilityReason.Should().NotBeNullOrWhiteSpace();
        unavailable[0].ReturnDate.Should().NotBeNull();
        unavailable[0].UnavailableSince.Should().NotBeNull();
    }

    [Fact]
    public void GenerateFullDataSet_ReturnsCompleteDataSet()
    {
        var dataSet = AustralianFakers.GenerateFullDataSet();

        dataSet.Mentors.Should().HaveCount(20);
        dataSet.Mentees.Should().HaveCount(30);
        dataSet.JobPostings.Should().HaveCount(15);
        dataSet.MeetupEvents.Should().HaveCount(7);
        dataSet.Notifications.Should().HaveCount(50);
    }
}
