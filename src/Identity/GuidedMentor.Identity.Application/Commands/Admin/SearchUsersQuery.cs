using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Searches and filters all user accounts with pagination.
/// Supports filtering by name, email, role, chapter, onboarding status, and account status.
/// </summary>
public sealed record SearchUsersQuery(
    Guid AdminId,
    string? Name,
    string? Email,
    string? Role,
    string? Chapter,
    string? OnboardingStatus,
    string? AccountStatus,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<SearchUsersResultDto>>;

/// <summary>
/// Paginated result of admin user search.
/// </summary>
public sealed record SearchUsersResultDto(
    IReadOnlyList<AdminUserDto> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
/// DTO representing a user in the admin search results.
/// </summary>
public sealed record AdminUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string? ActiveRole,
    string? Chapter,
    string OnboardingStatus,
    bool IsDisabled,
    bool IsLocked,
    DateTime CreatedAt);
