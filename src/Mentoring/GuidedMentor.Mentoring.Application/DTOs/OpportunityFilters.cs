using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// Filters for browsing opportunity postings.
/// </summary>
public sealed record OpportunityFilters(
    OpportunityType? Type = null,
    string? Location = null,
    IReadOnlyList<string>? Skills = null,
    ExperienceLevel? Experience = null);
