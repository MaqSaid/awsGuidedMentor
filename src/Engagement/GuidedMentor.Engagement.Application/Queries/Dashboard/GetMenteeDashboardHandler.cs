using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Queries.Dashboard;

/// <summary>
/// Aggregates mentee dashboard data from multiple sources with per-section error recovery.
/// Each section (sessions, recommendations, stats) is loaded independently;
/// a failure in one section does not block the others from rendering.
/// </summary>
public sealed class GetMenteeDashboardHandler : IRequestHandler<GetMenteeDashboardQuery, MenteeDashboardDto>
{
    private readonly IMenteeDashboardDataProvider _dataProvider;
    private readonly ILogger<GetMenteeDashboardHandler> _logger;

    public GetMenteeDashboardHandler(
        IMenteeDashboardDataProvider dataProvider,
        ILogger<GetMenteeDashboardHandler> logger)
    {
        _dataProvider = dataProvider;
        _logger = logger;
    }

    public async Task<MenteeDashboardDto> Handle(
        GetMenteeDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var sessionsTask = LoadSessionsAsync(request.UserId, cancellationToken);
        var recommendationsTask = LoadRecommendationsAsync(request.UserId, cancellationToken);
        var statsTask = LoadStatsAsync(request.UserId, cancellationToken);
        var summaryTask = LoadSummaryBarAsync(request.UserId, cancellationToken);

        await Task.WhenAll(sessionsTask, recommendationsTask, statsTask, summaryTask);

        var (sessions, sessionsError) = await sessionsTask;
        var (recommendations, recommendationsError) = await recommendationsTask;
        var (stats, statsError) = await statsTask;
        var (summary, _) = await summaryTask;

        // Apply empty state: if no active sessions and no error, sessions list is empty (empty state handled by frontend)
        return new MenteeDashboardDto(
            ActiveSessions: sessions,
            RecommendedMentors: recommendations,
            Stats: stats,
            SummaryBar: summary,
            SessionsError: sessionsError,
            RecommendationsError: recommendationsError,
            StatsError: statsError);
    }

    private async Task<(IReadOnlyList<ActiveSessionCardDto> Data, DashboardSectionErrorDto? Error)> LoadSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _dataProvider.GetActiveSessionsAsync(userId, cancellationToken);
            return (sessions, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active sessions for mentee {UserId}", userId);
            return (Array.Empty<ActiveSessionCardDto>(), new DashboardSectionErrorDto(
                Section: "sessions",
                Message: "Unable to load your active sessions. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(IReadOnlyList<RecommendedMentorDto> Data, DashboardSectionErrorDto? Error)> LoadRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var recommendations = await _dataProvider.GetRecommendedMentorsAsync(userId, limit: 3, cancellationToken);
            return (recommendations, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recommended mentors for mentee {UserId}", userId);
            return (Array.Empty<RecommendedMentorDto>(), new DashboardSectionErrorDto(
                Section: "recommendations",
                Message: "Unable to load mentor recommendations. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(MenteeProgressStatsDto Data, DashboardSectionErrorDto? Error)> LoadStatsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _dataProvider.GetProgressStatsAsync(userId, cancellationToken);
            return (stats, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load progress stats for mentee {UserId}", userId);
            return (new MenteeProgressStatsDto(0, 0, 0, 0), new DashboardSectionErrorDto(
                Section: "stats",
                Message: "Unable to load your progress statistics. Please try again.",
                CanRetry: true));
        }
    }

    private async Task<(MenteeSummaryBarDto Data, DashboardSectionErrorDto? Error)> LoadSummaryBarAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _dataProvider.GetSummaryBarAsync(userId, cancellationToken);
            return (summary, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load summary bar for mentee {UserId}", userId);
            return (new MenteeSummaryBarDto(0, 0, 0), new DashboardSectionErrorDto(
                Section: "summary",
                Message: "Unable to load summary information. Please try again.",
                CanRetry: true));
        }
    }
}
