using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.SharedKernel;
using ExperienceLevel = GuidedMentor.Mentoring.Domain.Entities.ExperienceLevel;

namespace GuidedMentor.Mentoring.Tests;

/// <summary>
/// Unit tests for the OpportunityPosting entity domain logic.
/// Validates Requirements 28.1-28.4, 28.9, 28.10.
/// </summary>
public sealed class OpportunityPostingTests
{
    private static readonly MentorId TestMentorId = new(Guid.NewGuid());

    private static OpportunityPosting CreateJobPosting(DateTime? publishedAt = null)
    {
        return OpportunityPosting.Create(
            mentorId: TestMentorId,
            title: "Senior AWS Developer",
            type: OpportunityType.Job,
            organisationName: "TechCorp Australia",
            description: new string('x', 150),
            location: "Sydney",
            eventDateTime: null,
            employmentType: EmploymentType.FullTime,
            requiredSkills: ["Lambda", "DynamoDB", "CDK"],
            requiredExperience: ExperienceLevel.Intermediate,
            externalUrl: "https://jobs.example.com/senior-aws",
            publishedAt: publishedAt);
    }

    private static OpportunityPosting CreateEventPosting(DateTime eventDate, DateTime? publishedAt = null)
    {
        return OpportunityPosting.Create(
            mentorId: TestMentorId,
            title: "AWS Community Day Sydney",
            type: OpportunityType.Event,
            organisationName: "AWS User Group Sydney",
            description: new string('x', 150),
            location: "Sydney",
            eventDateTime: eventDate,
            employmentType: null,
            requiredSkills: ["Lambda", "S3"],
            requiredExperience: ExperienceLevel.Any,
            externalUrl: "https://events.example.com/aws-day",
            publishedAt: publishedAt);
    }

    // ── Expiry Computation Tests ──

    [Fact]
    public void Create_JobPosting_ExpiryIsThirtyDaysFromPublish()
    {
        var publishDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var posting = CreateJobPosting(publishedAt: publishDate);

        posting.ExpiresAt.Should().Be(publishDate.AddDays(30));
    }

    [Fact]
    public void Create_EventPostingWithEarlyEventDate_ExpiryIsEventDate()
    {
        var publishDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var eventDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc); // 14 days out (before 30-day mark)

        var posting = CreateEventPosting(eventDate, publishedAt: publishDate);

        posting.ExpiresAt.Should().Be(eventDate);
    }

    [Fact]
    public void Create_EventPostingWithLateEventDate_ExpiryIsThirtyDays()
    {
        var publishDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var eventDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc); // 59 days out (after 30-day mark)

        var posting = CreateEventPosting(eventDate, publishedAt: publishDate);

        posting.ExpiresAt.Should().Be(publishDate.AddDays(30));
    }

    // ── Status Tests ──

    [Fact]
    public void Create_ShouldSetStatusActive()
    {
        var posting = CreateJobPosting();

        posting.Status.Should().Be(PostingStatus.Active);
        posting.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenBeforeExpiryDate_ShouldBeFalse()
    {
        var posting = CreateJobPosting(publishedAt: DateTime.UtcNow);

        posting.IsExpired.Should().BeFalse();
    }

    // ── Renew Tests ──

    [Fact]
    public void Renew_JobPosting_ShouldExtendExpiryByThirtyDays()
    {
        var posting = CreateJobPosting();

        var result = posting.Renew();

        result.IsSuccess.Should().BeTrue();
        posting.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        posting.Status.Should().Be(PostingStatus.Active);
    }

    [Fact]
    public void Renew_WorkshopPosting_ShouldFail()
    {
        var posting = OpportunityPosting.Create(
            mentorId: TestMentorId,
            title: "AWS Workshop 101",
            type: OpportunityType.Workshop,
            organisationName: "Training Co",
            description: new string('x', 150),
            location: "Online",
            eventDateTime: DateTime.UtcNow.AddDays(45),
            employmentType: null,
            requiredSkills: ["Lambda"],
            requiredExperience: ExperienceLevel.Beginner,
            externalUrl: "https://workshop.example.com/101");

        var result = posting.Renew();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only job postings can be renewed");
    }

    [Fact]
    public void Renew_EventPosting_ShouldFail()
    {
        var posting = CreateEventPosting(DateTime.UtcNow.AddDays(45));

        var result = posting.Renew();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only job postings can be renewed");
    }

    [Fact]
    public void Renew_TrainingPosting_ShouldFail()
    {
        var posting = OpportunityPosting.Create(
            mentorId: TestMentorId,
            title: "AWS Solutions Architect Training",
            type: OpportunityType.Training,
            organisationName: "CloudTraining Pty",
            description: new string('x', 150),
            location: "Melbourne",
            eventDateTime: DateTime.UtcNow.AddDays(45),
            employmentType: null,
            requiredSkills: ["EC2", "VPC"],
            requiredExperience: ExperienceLevel.Beginner,
            externalUrl: "https://training.example.com/sa");

        var result = posting.Renew();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only job postings can be renewed");
    }

    // ── Archive Tests ──

    [Fact]
    public void Archive_ShouldSetStatusToArchived()
    {
        var posting = CreateJobPosting();

        var result = posting.Archive();

        result.IsSuccess.Should().BeTrue();
        posting.Status.Should().Be(PostingStatus.Archived);
        posting.IsActive.Should().BeFalse();
    }

    // ── Domain Event Tests ──

    [Fact]
    public void Create_ShouldRaiseOpportunityPublishedEvent()
    {
        var posting = CreateJobPosting();

        posting.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OpportunityPublishedEvent>();

        var evt = (OpportunityPublishedEvent)posting.DomainEvents[0];
        evt.PostingId.Should().Be(posting.Id);
        evt.MentorId.Should().Be(TestMentorId);
        evt.Type.Should().Be(OpportunityType.Job);
        evt.RequiredSkills.Should().BeEquivalentTo(["Lambda", "DynamoDB", "CDK"]);
    }

    // ── Reconstitute Tests ──

    [Fact]
    public void Reconstitute_ShouldRecreateFromPersistedData()
    {
        var postingId = OpportunityPostingId.New();
        var publishedAt = DateTime.UtcNow.AddDays(-10);
        var expiresAt = publishedAt.AddDays(30);

        var posting = OpportunityPosting.Reconstitute(
            id: postingId,
            mentorId: TestMentorId,
            title: "Test Job",
            type: OpportunityType.Job,
            organisationName: "TestCorp",
            description: new string('x', 150),
            location: "Remote",
            eventDateTime: null,
            employmentType: EmploymentType.Contract,
            requiredSkills: ["S3"],
            requiredExperience: ExperienceLevel.Advanced,
            externalUrl: "https://example.com",
            publishedAt: publishedAt,
            expiresAt: expiresAt,
            status: PostingStatus.Active);

        posting.Id.Should().Be(postingId);
        posting.PostedByMentorId.Should().Be(TestMentorId);
        posting.Title.Should().Be("Test Job");
        posting.Type.Should().Be(OpportunityType.Job);
        posting.Status.Should().Be(PostingStatus.Active);
        posting.IsActive.Should().BeTrue();
    }

    // ── MarkExpired Tests ──

    [Fact]
    public void MarkExpired_ShouldSetStatusToExpired()
    {
        var posting = CreateJobPosting();

        posting.MarkExpired();

        posting.Status.Should().Be(PostingStatus.Expired);
        posting.IsActive.Should().BeFalse();
    }
}
