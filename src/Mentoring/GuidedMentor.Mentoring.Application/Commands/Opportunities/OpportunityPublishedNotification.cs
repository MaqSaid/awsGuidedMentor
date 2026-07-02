using GuidedMentor.Mentoring.Domain.Entities;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// MediatR notification wrapper for the <see cref="OpportunityPublishedEvent"/> domain event.
/// Domain events remain free of MediatR dependencies; this Application-layer wrapper
/// enables MediatR-based dispatch via the domain event dispatcher.
/// </summary>
public sealed record OpportunityPublishedNotification(OpportunityPublishedEvent DomainEvent) : INotification;
