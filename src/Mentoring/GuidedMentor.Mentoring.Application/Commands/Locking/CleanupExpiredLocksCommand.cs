using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Cleans up expired mentor locks that have not been automatically removed by DynamoDB TTL.
/// Triggered every 5 minutes by EventBridge Scheduler to ensure locks are released promptly
/// for accurate browse page availability.
/// </summary>
public sealed record CleanupExpiredLocksCommand : IRequest<Result>;
