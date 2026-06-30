import fc from 'fast-check';
import type { MentorProfile, CommunicationPreference } from '@guided-mentor/shared-types';
import {
  arbUuid,
  arbIsoTimestamp,
  arbAwsChapter,
  arbAvailabilityMap,
  arbAlphanumericString,
  AWS_SKILLS,
  AWS_CERTIFICATIONS,
  MENTORING_TOPICS,
} from './common';
import { arbCommunicationPreference } from './mentee-profiles';

/** Generates expertise areas from the predefined list (1-10 items) */
export const arbExpertiseAreas = (min = 1, max = 10): fc.Arbitrary<string[]> =>
  fc.shuffledSubarray([...AWS_SKILLS], { minLength: min, maxLength: max });

/** Generates certifications from the predefined list (0-15 items) */
export const arbCertifications = (min = 0, max = 15): fc.Arbitrary<string[]> =>
  fc.shuffledSubarray([...AWS_CERTIFICATIONS], { minLength: min, maxLength: Math.min(max, AWS_CERTIFICATIONS.length) });

/** Generates mentoring topics from the predefined list (1-10 items) */
export const arbTopics = (min = 1, max = 10): fc.Arbitrary<string[]> =>
  fc.shuffledSubarray([...MENTORING_TOPICS], { minLength: min, maxLength: max });

/** Generates session formats (1-3 communication preferences) */
export const arbSessionFormats = (): fc.Arbitrary<CommunicationPreference[]> =>
  fc.shuffledSubarray<CommunicationPreference>(['video_call', 'voice_call', 'chat'], { minLength: 1, maxLength: 3 });

/** Generates a valid mentor bio (100-1000 chars) */
export const arbBio = (): fc.Arbitrary<string> =>
  arbAlphanumericString(100, 1000);

/** Generates a valid professional title (2-100 chars) */
export const arbProfessionalTitle = (): fc.Arbitrary<string> =>
  fc.constantFrom(
    'Senior Cloud Architect',
    'AWS Solutions Architect',
    'Cloud Engineer',
    'DevOps Lead',
    'Principal Engineer',
    'Staff Software Engineer',
    'Cloud Security Specialist',
    'Data Engineer',
    'ML Engineer',
    'Platform Engineer',
  );

/** Generates a valid company name (2-100 chars) */
export const arbCompanyName = (): fc.Arbitrary<string> =>
  fc.constantFrom(
    'Amazon Web Services',
    'Google Cloud',
    'Microsoft',
    'Netflix',
    'Meta',
    'Spotify',
    'Atlassian',
    'Canva',
    'Stripe',
    'Cloudflare',
  );

/**
 * Generates a complete valid MentorProfile matching all constraints from the design document.
 *
 * Constraints:
 * - expertiseAreas: 1-10 items from predefined list
 * - certifications: 0-15 items from predefined list
 * - topics: 1-10 items from predefined list
 * - yearsOfExperience: 1-30
 * - maxMentees: 1-5
 * - activeMenteeCount: 0 to maxMentees
 * - professionalTitle: 2-100 chars
 * - companyName: 2-100 chars
 * - bio: 100-1000 chars
 * - sessionFormats: 1-3 communication preferences
 * - availability: at least one day with at least one slot
 */
export const arbMentorProfile = (): fc.Arbitrary<MentorProfile> =>
  fc.record({
    mentorId: arbUuid(),
    userId: arbUuid(),
    expertiseAreas: arbExpertiseAreas(1, 10),
    certifications: arbCertifications(0, 12),
    topics: arbTopics(1, 10),
    yearsOfExperience: fc.integer({ min: 1, max: 30 }),
    maxMentees: fc.integer({ min: 1, max: 5 }),
    activeMenteeCount: fc.constant(0), // Will be constrained below
    availability: arbAvailabilityMap(),
    sessionFormats: arbSessionFormats(),
    professionalTitle: arbProfessionalTitle(),
    companyName: arbCompanyName(),
    bio: arbBio(),
    onboardingStatus: fc.constant('completed' as const),
    isAvailable: fc.constant(true), // Will be derived
    createdAt: arbIsoTimestamp(),
    updatedAt: arbIsoTimestamp(),
  }).chain(profile =>
    // Ensure activeMenteeCount <= maxMentees
    fc.integer({ min: 0, max: profile.maxMentees }).map(count => ({
      ...profile,
      activeMenteeCount: count,
      isAvailable: count < profile.maxMentees,
    }))
  );

/**
 * Generates a mentor profile that is at full capacity (not available).
 */
export const arbFullCapacityMentor = (): fc.Arbitrary<MentorProfile> =>
  arbMentorProfile().map(profile => ({
    ...profile,
    activeMenteeCount: profile.maxMentees,
    isAvailable: false,
  }));

/**
 * Generates a mentor profile that has available slots.
 */
export const arbAvailableMentor = (): fc.Arbitrary<MentorProfile> =>
  arbMentorProfile().chain(profile =>
    fc.integer({ min: 0, max: Math.max(0, profile.maxMentees - 1) }).map(count => ({
      ...profile,
      activeMenteeCount: count,
      isAvailable: true,
    }))
  );

/**
 * Generates a partial mentor profile for matching algorithm tests.
 * Includes only the fields needed for compatibility scoring.
 */
export const arbMentorForMatching = (): fc.Arbitrary<{
  mentorId: string;
  displayName: string;
  awsChapter: string;
  expertiseAreas: string[];
  topics: string[];
  yearsOfExperience: number;
  maxMentees: number;
  activeMenteeCount: number;
}> =>
  fc.record({
    mentorId: arbUuid(),
    displayName: fc.constantFrom(
      'Alice Smith', 'Bob Jones', 'Charlie Brown', 'Diana Prince',
      'Eve Wilson', 'Frank Castle', 'Grace Hopper', 'Henry Ford',
    ),
    awsChapter: arbAwsChapter(),
    expertiseAreas: arbExpertiseAreas(1, 10),
    topics: arbTopics(1, 10),
    yearsOfExperience: fc.integer({ min: 1, max: 30 }),
    maxMentees: fc.integer({ min: 1, max: 5 }),
    activeMenteeCount: fc.constant(0),
  }).chain(mentor =>
    fc.integer({ min: 0, max: mentor.maxMentees }).map(count => ({
      ...mentor,
      activeMenteeCount: count,
    }))
  );
