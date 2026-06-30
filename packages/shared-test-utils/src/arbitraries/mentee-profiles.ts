import fc from 'fast-check';
import type { MenteeProfile, ExperienceLevel, PrimaryGoal, PreferredDuration, CommunicationPreference } from '@guided-mentor/shared-types';
import { arbUuid, arbIsoTimestamp, arbAwsChapter, arbSkills, arbAvailabilityMap, arbAlphanumericString } from './common';

/** Generates a valid experience level */
export const arbExperienceLevel = (): fc.Arbitrary<ExperienceLevel> =>
  fc.constantFrom<ExperienceLevel>('beginner', 'intermediate', 'advanced');

/** Generates a valid primary goal */
export const arbPrimaryGoal = (): fc.Arbitrary<PrimaryGoal> =>
  fc.constantFrom<PrimaryGoal>(
    'career_transition',
    'skill_development',
    'certification_preparation',
    'project_guidance'
  );

/** Generates a valid preferred duration */
export const arbPreferredDuration = (): fc.Arbitrary<PreferredDuration> =>
  fc.constantFrom<PreferredDuration>('4_weeks', '8_weeks', '12_weeks');

/** Generates a valid communication preference */
export const arbCommunicationPreference = (): fc.Arbitrary<CommunicationPreference> =>
  fc.constantFrom<CommunicationPreference>('video_call', 'voice_call', 'chat');

/** Generates a valid goal description (50-500 chars) */
export const arbGoalDescription = (): fc.Arbitrary<string> =>
  arbAlphanumericString(50, 500);

/**
 * Generates a complete valid MenteeProfile matching all constraints from the design document.
 *
 * Constraints:
 * - skills: 1-10 items from predefined list
 * - experienceLevel: beginner | intermediate | advanced
 * - yearsOfExperience: 0-50
 * - primaryGoal: one of 4 enum values
 * - goalDescription: 50-500 chars
 * - preferredDuration: 4_weeks | 8_weeks | 12_weeks
 * - availability: at least one day with at least one slot
 * - communicationPreference: video_call | voice_call | chat
 */
export const arbMenteeProfile = (): fc.Arbitrary<MenteeProfile> =>
  fc.record({
    menteeId: arbUuid(),
    userId: arbUuid(),
    skills: arbSkills(1, 10),
    experienceLevel: arbExperienceLevel(),
    yearsOfExperience: fc.integer({ min: 0, max: 50 }),
    primaryGoal: arbPrimaryGoal(),
    goalDescription: arbGoalDescription(),
    preferredDuration: arbPreferredDuration(),
    availability: arbAvailabilityMap(),
    communicationPreference: arbCommunicationPreference(),
    resumeUrl: fc.option(
      fc.constant('resumes/user123/20240101_resume.pdf'),
      { nil: undefined }
    ),
    onboardingStatus: fc.constant('completed' as const),
    currentLockId: fc.option(arbUuid(), { nil: null }),
    createdAt: arbIsoTimestamp(),
    updatedAt: arbIsoTimestamp(),
  });

/**
 * Generates a partial mentee profile for matching algorithm tests.
 * Includes only the fields needed for compatibility scoring.
 */
export const arbMenteeForMatching = (): fc.Arbitrary<Pick<MenteeProfile, 'skills' | 'yearsOfExperience' | 'primaryGoal'> & { awsChapter: string }> =>
  fc.record({
    skills: arbSkills(0, 10),
    yearsOfExperience: fc.integer({ min: 0, max: 50 }),
    primaryGoal: arbPrimaryGoal(),
    awsChapter: arbAwsChapter(),
  });
