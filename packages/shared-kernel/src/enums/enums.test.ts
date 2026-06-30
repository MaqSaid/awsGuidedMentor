import { describe, it, expect } from 'vitest';
import {
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

describe('Role', () => {
  it('has Mentor and Mentee values', () => {
    expect(Role.Mentor).toBe('Mentor');
    expect(Role.Mentee).toBe('Mentee');
  });
});

describe('AustralianChapter', () => {
  it('contains all 13 Australian chapters', () => {
    expect(AUSTRALIAN_CHAPTERS).toHaveLength(13);
  });

  it('includes all expected chapters', () => {
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Sydney);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Melbourne);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Brisbane);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Perth);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Adelaide);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Canberra);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Hobart);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Darwin);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.GoldCoast);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Newcastle);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Wollongong);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Geelong);
    expect(AUSTRALIAN_CHAPTERS).toContain(AustralianChapter.Townsville);
  });
});

describe('OnboardingStatus', () => {
  it('has all expected values', () => {
    expect(OnboardingStatus.NotStarted).toBe('NotStarted');
    expect(OnboardingStatus.InProgress).toBe('InProgress');
    expect(OnboardingStatus.Completed).toBe('Completed');
  });
});

describe('ExperienceLevel', () => {
  it('has all expected values', () => {
    expect(ExperienceLevel.Beginner).toBe('Beginner');
    expect(ExperienceLevel.Intermediate).toBe('Intermediate');
    expect(ExperienceLevel.Advanced).toBe('Advanced');
  });
});

describe('PrimaryGoal', () => {
  it('has all expected values', () => {
    expect(PrimaryGoal.CareerTransition).toBe('CareerTransition');
    expect(PrimaryGoal.SkillDevelopment).toBe('SkillDevelopment');
    expect(PrimaryGoal.CertificationPreparation).toBe('CertificationPreparation');
    expect(PrimaryGoal.ProjectGuidance).toBe('ProjectGuidance');
  });
});

describe('NotificationType', () => {
  it('has all expected values', () => {
    expect(NotificationType.RequestSent).toBe('RequestSent');
    expect(NotificationType.RequestAccepted).toBe('RequestAccepted');
    expect(NotificationType.RequestDeclined).toBe('RequestDeclined');
    expect(NotificationType.SessionPlanReady).toBe('SessionPlanReady');
    expect(NotificationType.CompletionMarked).toBe('CompletionMarked');
    expect(NotificationType.Reminder).toBe('Reminder');
    expect(NotificationType.OpportunityPosted).toBe('OpportunityPosted');
  });
});

describe('SessionStatus', () => {
  it('has all expected values', () => {
    expect(SessionStatus.PendingAcceptance).toBe('PendingAcceptance');
    expect(SessionStatus.PendingPlan).toBe('PendingPlan');
    expect(SessionStatus.Active).toBe('Active');
    expect(SessionStatus.MenteeCompleted).toBe('MenteeCompleted');
    expect(SessionStatus.Completed).toBe('Completed');
    expect(SessionStatus.Unresolved).toBe('Unresolved');
  });
});

describe('EmploymentType', () => {
  it('has all expected values', () => {
    expect(EmploymentType.FullTime).toBe('FullTime');
    expect(EmploymentType.PartTime).toBe('PartTime');
    expect(EmploymentType.Contract).toBe('Contract');
    expect(EmploymentType.Internship).toBe('Internship');
  });
});

describe('AvailabilityStatus', () => {
  it('has all expected values', () => {
    expect(AvailabilityStatus.Available).toBe('Available');
    expect(AvailabilityStatus.Unavailable).toBe('Unavailable');
  });
});

describe('UnavailabilityReason', () => {
  it('has all expected values', () => {
    expect(UnavailabilityReason.Vacation).toBe('Vacation');
    expect(UnavailabilityReason.PersonalCommitment).toBe('PersonalCommitment');
    expect(UnavailabilityReason.Workload).toBe('Workload');
    expect(UnavailabilityReason.Other).toBe('Other');
  });
});

describe('OpportunityType', () => {
  it('has all expected values', () => {
    expect(OpportunityType.Job).toBe('Job');
    expect(OpportunityType.Workshop).toBe('Workshop');
    expect(OpportunityType.Event).toBe('Event');
    expect(OpportunityType.Training).toBe('Training');
  });
});

describe('PostingStatus', () => {
  it('has all expected values', () => {
    expect(PostingStatus.Active).toBe('Active');
    expect(PostingStatus.Archived).toBe('Archived');
    expect(PostingStatus.Expired).toBe('Expired');
  });
});
