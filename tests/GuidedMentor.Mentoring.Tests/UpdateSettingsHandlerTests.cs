using GuidedMentor.Mentoring.Application.Commands.Settings;
using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;
using AustralianChapter = GuidedMentor.SharedKernel.AustralianChapter;

namespace GuidedMentor.Mentoring.Tests;

/// <summary>
/// Unit tests for the UpdateSettingsHandler.
/// Validates Requirements 13.2, 13.6, 13.7.
/// </summary>
public sealed class UpdateSettingsHandlerTests
{
    private readonly FakeMentorRepository _repository = new();

    private static UpdateSettingsCommand CreateValidCommand(
        Guid mentorId,
        int maxMentees = 3,
        AustralianChapter chapter = AustralianChapter.Sydney)
    {
        return new UpdateSettingsCommand(
            MentorId: mentorId,
            DisplayName: "Jane Smith",
            ProfessionalTitle: "Senior Cloud Architect",
            CompanyName: "AWS Solutions",
            Chapter: chapter,
            ExpertiseAreas: new List<string> { "Lambda", "DynamoDB", "CloudFormation" },
            YearsOfExperience: 8,
            Certifications: new List<string> { "Solutions Architect Professional" },
            Topics: new List<string> { "Serverless", "IaC" },
            MaxMentees: maxMentees,
            SessionFormats: new List<string> { "video_call", "chat" },
            Bio: new string('A', 150)); // Meets 100-1000 char requirement
    }

    private MentorEntity CreateMentor(
        Guid? id = null,
        int maxMentees = 3,
        int activeMenteeCount = 1,
        AustralianChapter? chapter = null)
    {
        var mentorId = id ?? Guid.NewGuid();
        var mentor = MentorEntity.Create(
            mentorId,
            "Jane Smith",
            maxMentees,
            activeMenteeCount,
            chapter: chapter ?? AustralianChapter.Sydney);
        _repository.Add(mentor);
        return mentor;
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSucceed()
    {
        var mentor = CreateMentor(activeMenteeCount: 2);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, maxMentees: 4);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MentorNotFound_ShouldFail()
    {
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Req 13.6: maxMentees constraint ──

    [Fact]
    public async Task Handle_MaxMenteesEqualToActiveMenteeCount_ShouldSucceed()
    {
        var mentor = CreateMentor(activeMenteeCount: 3);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, maxMentees: 3);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MaxMenteesAboveActiveMenteeCount_ShouldSucceed()
    {
        var mentor = CreateMentor(activeMenteeCount: 2);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, maxMentees: 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MaxMenteesBelowActiveMenteeCount_ShouldFail()
    {
        var mentor = CreateMentor(activeMenteeCount: 3);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, maxMentees: 2);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot reduce maxMentees");
        result.Error.Should().Contain("3 active mentee");
    }

    [Fact]
    public async Task Handle_MaxMenteesOneWithZeroActive_ShouldSucceed()
    {
        var mentor = CreateMentor(activeMenteeCount: 0);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, maxMentees: 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Req 13.7: compatibility score recalculation on chapter change ──

    [Fact]
    public async Task Handle_ChapterChange_ShouldFlagForRecalculation()
    {
        var mentor = CreateMentor(chapter: AustralianChapter.Sydney);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, chapter: AustralianChapter.Melbourne);

        await handler.Handle(command, CancellationToken.None);

        var saved = _repository.LastSaved!;
        saved.RequiresCompatibilityRecalculation.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SameChapter_ShouldNotFlagForRecalculation()
    {
        var mentor = CreateMentor(chapter: AustralianChapter.Sydney);
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id, chapter: AustralianChapter.Sydney);

        await handler.Handle(command, CancellationToken.None);

        var saved = _repository.LastSaved!;
        saved.RequiresCompatibilityRecalculation.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SuccessfulUpdate_ShouldCallSaveSettings()
    {
        var mentor = CreateMentor();
        var handler = new UpdateSettingsHandler(_repository);
        var command = CreateValidCommand(mentor.Id);

        await handler.Handle(command, CancellationToken.None);

        _repository.SaveSettingsCalled.Should().BeTrue();
    }

    // ── Fake repository for testing ──

    private sealed class FakeMentorRepository : IMentorRepository
    {
        private readonly Dictionary<Guid, MentorEntity> _mentors = new();
        public MentorEntity? LastSaved { get; private set; }
        public bool SaveSettingsCalled { get; private set; }

        public void Add(MentorEntity mentor) => _mentors[mentor.Id] = mentor;

        public Task<MentorEntity?> GetByIdAsync(Guid mentorId, CancellationToken ct = default)
        {
            _mentors.TryGetValue(mentorId, out var mentor);
            return Task.FromResult(mentor);
        }

        public Task SaveSettingsAsync(MentorEntity mentor, CancellationToken ct = default)
        {
            LastSaved = mentor;
            SaveSettingsCalled = true;
            return Task.CompletedTask;
        }

        public Task SaveAvailabilityAsync(MentorEntity mentor, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<MentorAvailability?> GetAvailabilityAsync(Guid mentorId, CancellationToken ct = default)
            => Task.FromResult<MentorAvailability?>(null);

        public Task<IReadOnlyList<MentorEntity>> GetUnavailableMentorsOverDaysAsync(int days, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MentorEntity>>(Array.Empty<MentorEntity>());

        public Task IncrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DecrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
