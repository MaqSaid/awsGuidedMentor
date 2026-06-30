using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// Data transfer object for opportunity posting data.
/// </summary>
public sealed record OpportunityPostingDto(
    Guid PostingId,
    Guid MentorId,
    string Title,
    OpportunityType Type,
    string OrganisationName,
    string Description,
    string Location,
    DateTime? EventDateTime,
    EmploymentType? EmploymentType,
    IReadOnlyList<string> RequiredSkills,
    ExperienceLevel RequiredExperience,
    string ExternalUrl,
    DateTime PublishedAt,
    DateTime ExpiresAt,
    PostingStatus Status,
    bool IsActive,
    int DaysRemaining)
{
    /// <summary>
    /// Maps from domain entity to DTO.
    /// </summary>
    public static OpportunityPostingDto FromDomain(OpportunityPosting posting)
    {
        var daysRemaining = Math.Max(0, (int)(posting.ExpiresAt - DateTime.UtcNow).TotalDays);

        return new OpportunityPostingDto(
            PostingId: posting.Id.Value,
            MentorId: posting.PostedByMentorId.Value,
            Title: posting.Title,
            Type: posting.Type,
            OrganisationName: posting.OrganisationName,
            Description: posting.Description,
            Location: posting.Location,
            EventDateTime: posting.EventDateTime,
            EmploymentType: posting.EmploymentType,
            RequiredSkills: posting.RequiredSkills,
            RequiredExperience: posting.RequiredExperience,
            ExternalUrl: posting.ExternalUrl,
            PublishedAt: posting.PublishedAt,
            ExpiresAt: posting.ExpiresAt,
            Status: posting.Status,
            IsActive: posting.IsActive,
            DaysRemaining: daysRemaining);
    }
}
