using GuidedMentor.Mentoring.Application.Commands.Settings;
using GuidedMentor.SharedKernel;
using AustralianChapter = GuidedMentor.SharedKernel.AustralianChapter;

namespace GuidedMentor.Mentoring.Tests;

/// <summary>
/// Unit tests for the UpdateSettingsValidator.
/// Ensures the same onboarding validation rules are applied to settings updates (Req 13.2).
/// </summary>
public sealed class UpdateSettingsValidatorTests
{
    private readonly UpdateSettingsValidator _validator = new();

    private static UpdateSettingsCommand CreateValidCommand()
    {
        return new UpdateSettingsCommand(
            MentorId: Guid.NewGuid(),
            DisplayName: "Jane Smith",
            ProfessionalTitle: "Senior Cloud Architect",
            CompanyName: "AWS Solutions",
            Chapter: AustralianChapter.Sydney,
            ExpertiseAreas: new List<string> { "Lambda", "DynamoDB", "CloudFormation" },
            YearsOfExperience: 8,
            Certifications: new List<string> { "Solutions Architect Professional" },
            Topics: new List<string> { "Serverless", "IaC" },
            MaxMentees: 3,
            SessionFormats: new List<string> { "video_call", "chat" },
            Bio: new string('A', 150)); // Meets 100-1000 char requirement
    }

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // ── DisplayName rules (2-100 chars) ──

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void DisplayName_TooShort_ShouldFail(string name)
    {
        var command = CreateValidCommand() with { DisplayName = name };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void DisplayName_TooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { DisplayName = new string('X', 101) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── ProfessionalTitle rules (2-100 chars) ──

    [Fact]
    public void ProfessionalTitle_Empty_ShouldFail()
    {
        var command = CreateValidCommand() with { ProfessionalTitle = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProfessionalTitle");
    }

    [Fact]
    public void ProfessionalTitle_TooShort_ShouldFail()
    {
        var command = CreateValidCommand() with { ProfessionalTitle = "A" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── CompanyName rules (2-100 chars) ──

    [Fact]
    public void CompanyName_Empty_ShouldFail()
    {
        var command = CreateValidCommand() with { CompanyName = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    // ── ExpertiseAreas rules (1-10 items) ──

    [Fact]
    public void ExpertiseAreas_Empty_ShouldFail()
    {
        var command = CreateValidCommand() with { ExpertiseAreas = new List<string>() };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpertiseAreas");
    }

    [Fact]
    public void ExpertiseAreas_MoreThan10_ShouldFail()
    {
        var areas = Enumerable.Range(1, 11).Select(i => $"Area{i}").ToList();
        var command = CreateValidCommand() with { ExpertiseAreas = areas };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── YearsOfExperience rules (1-30) ──

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void YearsOfExperience_OutOfRange_ShouldFail(int years)
    {
        var command = CreateValidCommand() with { YearsOfExperience = years };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "YearsOfExperience");
    }

    // ── Topics rules (1-10 items) ──

    [Fact]
    public void Topics_Empty_ShouldFail()
    {
        var command = CreateValidCommand() with { Topics = new List<string>() };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Topics");
    }

    [Fact]
    public void Topics_MoreThan10_ShouldFail()
    {
        var topics = Enumerable.Range(1, 11).Select(i => $"Topic{i}").ToList();
        var command = CreateValidCommand() with { Topics = topics };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── MaxMentees rules (1-5) ──

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void MaxMentees_OutOfRange_ShouldFail(int maxMentees)
    {
        var command = CreateValidCommand() with { MaxMentees = maxMentees };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxMentees");
    }

    // ── SessionFormats rules (at least 1, valid values) ──

    [Fact]
    public void SessionFormats_Empty_ShouldFail()
    {
        var command = CreateValidCommand() with { SessionFormats = new List<string>() };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionFormats");
    }

    [Fact]
    public void SessionFormats_InvalidValue_ShouldFail()
    {
        var command = CreateValidCommand() with { SessionFormats = new List<string> { "invalid_format" } };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SessionFormats_AllValid_ShouldPass()
    {
        var command = CreateValidCommand() with { SessionFormats = new List<string> { "video_call", "voice_call", "chat" } };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // ── Bio rules (100-1000 chars) ──

    [Fact]
    public void Bio_TooShort_ShouldFail()
    {
        var command = CreateValidCommand() with { Bio = new string('X', 99) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Bio");
    }

    [Fact]
    public void Bio_TooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { Bio = new string('X', 1001) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bio_AtMinBoundary_ShouldPass()
    {
        var command = CreateValidCommand() with { Bio = new string('X', 100) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bio_AtMaxBoundary_ShouldPass()
    {
        var command = CreateValidCommand() with { Bio = new string('X', 1000) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // ── Certifications rules (max 15) ──

    [Fact]
    public void Certifications_MoreThan15_ShouldFail()
    {
        var certs = Enumerable.Range(1, 16).Select(i => $"Cert{i}").ToList();
        var command = CreateValidCommand() with { Certifications = certs };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Certifications_EmptyList_ShouldPass()
    {
        var command = CreateValidCommand() with { Certifications = new List<string>() };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
