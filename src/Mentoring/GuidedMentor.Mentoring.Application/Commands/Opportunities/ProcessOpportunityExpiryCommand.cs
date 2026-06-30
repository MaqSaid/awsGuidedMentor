using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Triggered by EventBridge daily scheduler.
/// Archives expired/past-event postings and notifies mentors with renewal option (jobs only).
/// </summary>
public sealed record ProcessOpportunityExpiryCommand : IRequest<Result>;
