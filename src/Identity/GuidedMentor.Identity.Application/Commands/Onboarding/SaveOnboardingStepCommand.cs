using System.Text.Json;
using GuidedMentor.Identity.Application.DTOs.Onboarding;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Onboarding;

/// <summary>
/// Saves a single onboarding step's data for a user.
/// Validates step data based on role and step number.
/// On final step completion, persists to Mentors_Table/Mentees_Table and sets onboardingStatus=Completed.
/// </summary>
public sealed record SaveOnboardingStepCommand(
    Guid UserId,
    GuidedMentor.SharedKernel.Role Role,
    int Step,
    JsonDocument Data) : IRequest<Result<SaveOnboardingStepResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"User:{UserId}:Onboarding:Step{Step}";
}
