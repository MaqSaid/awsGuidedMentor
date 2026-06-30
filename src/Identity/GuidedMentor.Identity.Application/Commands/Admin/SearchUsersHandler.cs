using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Handles searching and filtering users with pagination.
/// Verifies the requester is a Super Admin before returning results.
/// </summary>
public sealed class SearchUsersHandler : IRequestHandler<SearchUsersQuery, Result<SearchUsersResultDto>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAdminUserSearchService _searchService;

    public SearchUsersHandler(
        IAdminRepository adminRepository,
        IAdminUserSearchService searchService)
    {
        _adminRepository = adminRepository;
        _searchService = searchService;
    }

    public async Task<Result<SearchUsersResultDto>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result<SearchUsersResultDto>.Failure(
                "Admin account not found. Only Super Admins can search users.");
        }

        var filters = new UserSearchFilters(
            Name: request.Name,
            Email: request.Email,
            Role: request.Role,
            Chapter: request.Chapter,
            OnboardingStatus: request.OnboardingStatus,
            AccountStatus: request.AccountStatus);

        var result = await _searchService.SearchUsersAsync(
            filters,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result<SearchUsersResultDto>.Success(result);
    }
}
