using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Availability;

/// <summary>
/// Retrieves the current availability status for a mentor.
/// </summary>
public sealed record GetMentorAvailabilityQuery(Guid MentorId) : IRequest<Result<MentorAvailabilityDto>>;
