using System.Text.Json;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Settings;

/// <summary>
/// Updates settings/profile for the currently active role.
/// Validates all inputs using the same rules as onboarding.
/// For mentors, enforces maxMentees ≥ current activeMenteeCount.
/// On chapter change, raises ChapterChangedEvent for compatibility score recalculation.
/// </summary>
public sealed record UpdateSettingsCommand(
    Guid UserId,
    Role Role,
    JsonDocument Data) : IRequest<Result<UpdateSettingsResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"User:{UserId}:Settings:{Role}";
}
