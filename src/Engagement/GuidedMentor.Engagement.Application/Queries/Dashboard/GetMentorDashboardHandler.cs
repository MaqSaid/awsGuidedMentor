using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Queries.Dashboard;

/// <summary>
/// Aggregates mentor dashboard data from multiple sources with per-section error recovery.
/// Each section (requests, mentees, capacity) is loaded independently;
/// a failure in one section does not block the others from rendering.
/// </summary>
public sealed class GetMentorDashboardHandler : IRequestHandler<GetMentorDashboardQuery, MentorDashboardDto>
{
    private readonly IMentorDashboardDataProvider _dataProvider;
    private readonly ILogger<GetMentorDashboardHandler> _logger;

    public GetMentorDashboardHandler(
        IMentorDashboardDataProvider dataProvider,
        ILogger<GetMentorDashboardHandler> logger)
    {
        _dataProvider = dataProvider;
        _logger = logger;
    }

    public async Task<MentorDashboardDto> Handle(
        GetMentorDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var requestsTask = LoadPendingRequestsAsync(request.UserId, cancellationToken);
        var menteesTask = LoadActiveMenteesAsync(request.UserId, cancellationToken);
        var capacityTask = LoadCapacityAsync(request.UserId, cancellationToken);
        var availabilityTask = LoadAvailabilityAsync(request.UserId, cancellationToken);

        await Task.WhenAll(requestsTask, menteesTask, capacityTask, availabilityTask);

        var (requests, requestsError) = await requestsTask;
        var (mentees, menteesError) = await menteesTask;
        var (capacity, capacityError) = await capacityTask;
        var (availability, _) = await availabilityTask;

        return new MentorDashboardDto(
            PendingRequests: requests,
            ActiveMentees: mentees,
            Capacity: capacity,
            AvailabilityStatus: availability,
            RequestsError: requestsError,
            MenteesError: menteesError,
            CapacityError: capacityError);
    }

    private async Task<(IReadOnlyList<PendingRequestDto> Data, DashboardSectionErrorDto? Error)> LoadPendingRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _dataProvider.GetPendingRequestsAsync(userId, cancellationToken);
            return (requests, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pending requests for mentor {UserId}", userId);
            return (Array.Empty<PendingRequestDto>(), new DashboardSectionErrorDto(
                Section: "requests",
                Message: "Unable to load pending requests. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(IReadOnlyList<ActiveMenteeCardDto> Data, DashboardSectionErrorDto? Error)> LoadActiveMenteesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var mentees = await _dataProvider.GetActiveMenteesAsync(userId, cancellationToken);
            return (mentees, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active mentees for mentor {UserId}", userId);
            return (Array.Empty<ActiveMenteeCardDto>(), new DashboardSectionErrorDto(
                Section: "mentees",
                Message: "Unable to load your active mentees. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(CapacityIndicatorDto Data, DashboardSectionErrorDto? Error)> LoadCapacityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var capacity = await _dataProvider.GetCapacityAsync(userId, cancellationToken);
            return (capacity, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load capacity for mentor {UserId}", userId);
            return (new CapacityIndicatorDto(0, 0, false), new DashboardSectionErrorDto(
                Section: "capacity",
                Message: "Unable to load capacity information. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(AvailabilityStatusDto Data, DashboardSectionErrorDto? Error)> LoadAvailabilityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var availability = await _dataProvider.GetAvailabilityStatusAsync(userId, cancellationToken);
            return (availability, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load availability status for mentor {UserId}", userId);
            return (new AvailabilityStatusDto(true, null, null), new DashboardSectionErrorDto(
                Section: "availability",
                Message: "Unable to load availability status. Please try again.",
                CanRetry: true));
        }
    }
}
