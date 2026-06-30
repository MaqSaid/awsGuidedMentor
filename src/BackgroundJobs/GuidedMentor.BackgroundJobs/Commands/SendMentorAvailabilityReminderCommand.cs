using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Triggered daily by EventBridge Scheduler.
/// Queries all mentors who have been Unavailable for more than 90 days
/// and sends a reminder notification asking if they wish to remain on the platform
/// or deactivate their mentor profile.
///
/// Requirements: 32.7
/// </summary>
public sealed record SendMentorAvailabilityReminderCommand : IRequest<Result>;
