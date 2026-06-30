using GuidedMentor.Mentoring.Domain.ValueObjects;

namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// Response DTO for the mentor's current availability status.
/// </summary>
public sealed record MentorAvailabilityDto(
    string Status,
    string? Reason,
    DateTime? ReturnDate,
    DateTime? UnavailableSince)
{
    public static MentorAvailabilityDto FromDomain(MentorAvailability availability)
    {
        return new MentorAvailabilityDto(
            availability.Status.ToString().ToLowerInvariant(),
            availability.Reason?.ToString().ToLowerInvariant(),
            availability.ReturnDate,
            availability.UnavailableSince);
    }
}
