using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Handles the lightweight API-triggered session plan generation request.
/// Fetches participant profiles, then delegates to the full GenerateSessionPlanCommand pipeline.
/// </summary>
public sealed class RequestSessionPlanGenerationHandler
    : IRequestHandler<RequestSessionPlanGenerationCommand, Result>
{
    private readonly IProfileProvider _profileProvider;
    private readonly IMediator _mediator;
    private readonly ILogger<RequestSessionPlanGenerationHandler> _logger;

    public RequestSessionPlanGenerationHandler(
        IProfileProvider profileProvider,
        IMediator mediator,
        ILogger<RequestSessionPlanGenerationHandler> logger)
    {
        _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        RequestSessionPlanGenerationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing session plan generation request for SessionId={SessionId}, UserId={UserId}",
            request.SessionId, request.RequestingUserId);

        // Fetch session participants
        var participants = await _profileProvider.GetSessionParticipantsAsync(request.SessionId, cancellationToken);
        if (participants is null)
        {
            return Result.Failure("Session not found or participants could not be resolved.");
        }

        var (menteeId, mentorId) = participants.Value;

        // Fetch profiles
        var menteeProfile = await _profileProvider.GetMenteeProfileAsync(menteeId, cancellationToken);
        if (menteeProfile is null)
        {
            return Result.Failure("Mentee profile not found.");
        }

        var mentorProfile = await _profileProvider.GetMentorProfileAsync(mentorId, cancellationToken);
        if (mentorProfile is null)
        {
            return Result.Failure("Mentor profile not found.");
        }

        // Delegate to the full GenerateSessionPlanCommand
        var command = new GenerateSessionPlanCommand(
            request.SessionId, menteeId, mentorId, menteeProfile, mentorProfile);

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
