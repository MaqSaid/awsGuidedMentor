using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Triggered hourly by EventBridge Scheduler.
/// Aggregates raw engagement events from DynamoDB EngagementEvents_Table
/// into Aurora PostgreSQL materialized views and summary tables.
/// Computes: DAU/WAU/MAU counts, feature heatmaps, error hotspots,
/// retention metrics, and conversion funnel data.
///
/// Requirements: 20.7
/// </summary>
public sealed record AggregateAnalyticsCommand : IRequest<Result>;
