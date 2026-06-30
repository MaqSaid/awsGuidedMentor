using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Triggered daily at 9:00 AM AEST by EventBridge Scheduler.
/// Compiles unread notifications from the past 24 hours for each user
/// and sends a consolidated digest notification (email or push).
///
/// Requirements: 20.7
/// </summary>
public sealed record SendNotificationDigestCommand : IRequest<Result>;
