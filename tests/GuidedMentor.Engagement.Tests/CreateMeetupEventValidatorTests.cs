using GuidedMentor.Engagement.Application.Commands.Meetups;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for CreateMeetupEventValidator.
/// Validates: Requirements 29.1, 29.7
/// </summary>
public sealed class CreateMeetupEventValidatorTests
{
    private readonly CreateMeetupEventValidator _validator = new();

    private static CreateMeetupEventCommand CreateValidCommand() => new(
        ChapterLeadId: Guid.NewGuid(),
        Chapter: AustralianChapter.Sydney,
        Title: "AWS Sydney Meetup - Serverless",
        EventDate: DateTime.UtcNow.AddDays(14),
        StartTime: new TimeOnly(18, 0),
        EndTime: new TimeOnly(20, 0),
        VenueName: "AWS Office Sydney",
        VenueAddress: "200 George Street, Sydney NSW 2000",
        EventUrl: "https://meetup.com/aws-sydney/events/12345");

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.Validate(CreateValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTitle_FailsValidation()
    {
        var command = CreateValidCommand() with { Title = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Theory]
    [InlineData("ABCD")]  // Too short (4 chars, min 5)
    [InlineData("")]       // Empty
    public void TitleTooShort_FailsValidation(string title)
    {
        var command = CreateValidCommand() with { Title = title };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_FailsValidation()
    {
        var command = CreateValidCommand() with { Title = new string('A', 201) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PastEventDate_FailsValidation()
    {
        var command = CreateValidCommand() with { EventDate = DateTime.UtcNow.AddDays(-1) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventDate");
    }

    [Fact]
    public void StartTimeAfterEndTime_FailsValidation()
    {
        var command = CreateValidCommand() with
        {
            StartTime = new TimeOnly(20, 0),
            EndTime = new TimeOnly(18, 0)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime");
    }

    [Fact]
    public void VenueNameTooShort_FailsValidation()
    {
        var command = CreateValidCommand() with { VenueName = "X" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VenueName");
    }

    [Fact]
    public void VenueAddressTooShort_FailsValidation()
    {
        var command = CreateValidCommand() with { VenueAddress = "ABC" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VenueAddress");
    }

    [Fact]
    public void InvalidEventUrl_FailsValidation()
    {
        var command = CreateValidCommand() with { EventUrl = "http://not-https.com" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventUrl");
    }

    [Fact]
    public void NonUrlEventUrl_FailsValidation()
    {
        var command = CreateValidCommand() with { EventUrl = "not-a-url" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyChapterLeadId_FailsValidation()
    {
        var command = CreateValidCommand() with { ChapterLeadId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ChapterLeadId");
    }
}
