/**
 * Shared domain enums for the GuidedMentor platform.
 * These map to the DDD domain model and are used across bounded contexts.
 */

/** User role in the platform */
export enum Role {
  Mentor = 'Mentor',
  Mentee = 'Mentee',
}

/**
 * AWS User Group chapters across Australia.
 * The platform supports cross-city matching (all chapters visible).
 */
export enum AustralianChapter {
  Sydney = 'Sydney',
  Melbourne = 'Melbourne',
  Brisbane = 'Brisbane',
  Perth = 'Perth',
  Adelaide = 'Adelaide',
  Canberra = 'Canberra',
  Hobart = 'Hobart',
  Darwin = 'Darwin',
  GoldCoast = 'GoldCoast',
  Newcastle = 'Newcastle',
  Wollongong = 'Wollongong',
  Geelong = 'Geelong',
  Townsville = 'Townsville',
}

/** All Australian chapters as an array for validation and iteration */
export const AUSTRALIAN_CHAPTERS = Object.values(AustralianChapter);

/** Onboarding progress status */
export enum OnboardingStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed',
}

/** Mentee experience level */
export enum ExperienceLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
}

/** Primary goal a mentee seeks to achieve */
export enum PrimaryGoal {
  CareerTransition = 'CareerTransition',
  SkillDevelopment = 'SkillDevelopment',
  CertificationPreparation = 'CertificationPreparation',
  ProjectGuidance = 'ProjectGuidance',
}

/** Types of notifications delivered to users */
export enum NotificationType {
  RequestSent = 'RequestSent',
  RequestAccepted = 'RequestAccepted',
  RequestDeclined = 'RequestDeclined',
  SessionPlanReady = 'SessionPlanReady',
  CompletionMarked = 'CompletionMarked',
  Reminder = 'Reminder',
  OpportunityPosted = 'OpportunityPosted',
}

/** Session lifecycle status */
export enum SessionStatus {
  PendingAcceptance = 'PendingAcceptance',
  PendingPlan = 'PendingPlan',
  Active = 'Active',
  MenteeCompleted = 'MenteeCompleted',
  Completed = 'Completed',
  Unresolved = 'Unresolved',
}

/** Employment type for opportunity postings */
export enum EmploymentType {
  FullTime = 'FullTime',
  PartTime = 'PartTime',
  Contract = 'Contract',
  Internship = 'Internship',
}

/** Mentor availability toggle status */
export enum AvailabilityStatus {
  Available = 'Available',
  Unavailable = 'Unavailable',
}

/** Reason for mentor being unavailable */
export enum UnavailabilityReason {
  Vacation = 'Vacation',
  PersonalCommitment = 'PersonalCommitment',
  Workload = 'Workload',
  Other = 'Other',
}

/** Type of opportunity posting on the Opportunities Board */
export enum OpportunityType {
  Job = 'Job',
  Workshop = 'Workshop',
  Event = 'Event',
  Training = 'Training',
}

/** Status of an opportunity posting */
export enum PostingStatus {
  Active = 'Active',
  Archived = 'Archived',
  Expired = 'Expired',
}
