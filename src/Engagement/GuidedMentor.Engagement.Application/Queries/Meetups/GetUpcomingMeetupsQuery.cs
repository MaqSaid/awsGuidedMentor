using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Meetups;

/// <summary>
/// Gets upcoming meetup events for a chapter, sorted by eventDate ascending.
/// Excludes cancelled and past events. Limited to max 3 results by default.
/// Uses GSI-Chapter (PK=chapter, SK=eventDate).
/// </summary>
public sealed record GetUpcomingMeetupsQuery(
    AustralianChapter Chapter,
    int Limit = 3) : IRequest<Result<IReadOnlyList<MeetupEventDto>>>;
