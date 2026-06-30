/**
 * GuidedMentor SharedKernel
 *
 * Domain kernel and cross-cutting DDD building blocks.
 * This package has ZERO dependencies on any AWS SDK, infrastructure,
 * or third-party packages (except System/built-in types).
 *
 * Contains:
 * - Base classes: Entity<T>, AggregateRoot<T>, ValueObject, Result<T>
 * - Interfaces: IDomainEvent, IRepository<T>
 * - Domain enums: Role, AustralianChapter, OnboardingStatus, etc.
 * - Value objects: UserId, MentorId, MenteeId, SessionId, Email, etc.
 */

// Base DDD building blocks
export {
  IDomainEvent,
  DomainEvent,
  Entity,
  AggregateRoot,
  ValueObject,
  Result,
  IRepository,
} from './base';

// Domain enums
export {
  Role,
  AustralianChapter,
  AUSTRALIAN_CHAPTERS,
  OnboardingStatus,
  ExperienceLevel,
  PrimaryGoal,
  NotificationType,
  SessionStatus,
  EmploymentType,
  AvailabilityStatus,
  UnavailabilityReason,
  OpportunityType,
  PostingStatus,
} from './enums';

// Value objects
export {
  UserId,
  MentorId,
  MenteeId,
  SessionId,
  Email,
  OpportunityPostingId,
  MeetupEventId,
  NotificationId,
  LockId,
  AdminUserId,
} from './value-objects';
