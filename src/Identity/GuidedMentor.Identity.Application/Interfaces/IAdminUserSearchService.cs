using GuidedMentor.Identity.Application.Commands.Admin;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Service for searching and filtering users in the admin context.
/// Implementation queries DynamoDB/Aurora with the specified filters and pagination.
/// </summary>
public interface IAdminUserSearchService
{
    /// <summary>
    /// Searches users with the given filters and returns a paginated result.
    /// </summary>
    Task<SearchUsersResultDto> SearchUsersAsync(
        UserSearchFilters filters,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

/// <summary>
/// Represents the search filters for admin user search.
/// </summary>
public sealed record UserSearchFilters(
    string? Name,
    string? Email,
    string? Role,
    string? Chapter,
    string? OnboardingStatus,
    string? AccountStatus);
