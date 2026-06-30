using GuidedMentor.Content.Application.Plugins.Dtos;
using GuidedMentor.Content.Domain;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Command to generate a personalised session plan using AI (Bedrock Claude Sonnet 4).
/// Triggered when a mentor accepts a mentorship request (session transitions to PendingPlan).
/// 
/// Validates: Requirements 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 24.5
/// </summary>
public sealed record GenerateSessionPlanCommand(
    Guid SessionId,
    Guid MenteeId,
    Guid MentorId,
    MenteeProfileDto MenteeProfile,
    MentorProfileDto MentorProfile) : IRequest<Result<SessionPlan>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}:Plan";
}
