/**
 * DynamoDB entity types matching the table schemas defined in the design document.
 */

import {
  AvailabilityMap,
  CommunicationPreference,
  ExperienceLevel,
  NotificationType,
  OnboardingStatus,
  PreferredDuration,
  PrimaryGoal,
  SessionFormat,
  SessionStatus,
  UserRole,
} from './enums';

// --- Users Table ---

export interface UserEntity {
  /** Partition key - Cognito sub (UUID) */
  userId: string;
  /** GSI-Email partition key */
  email: string;
  role: UserRole;
  displayName: string;
  profilePhotoUrl: string;
  awsChapter: string;
  location: string;
  onboardingStatus: OnboardingStatus;
  failedLoginAttempts: number;
  /** ISO 8601 timestamp, null if not locked */
  lockedUntil: string | null;
  /** ISO 8601 timestamp */
  createdAt: string;
  /** ISO 8601 timestamp */
  updatedAt: string;
}

// --- Mentors Table ---

export interface MentorEntity {
  /** Partition key - UUID */
  mentorId: string;
  /** GSI-UserId partition key - FK to Users table */
  userId: string;
  /** Max 10 items */
  expertiseAreas: string[];
  /** Max 15 items */
  certifications: string[];
  /** Max 10 mentoring topics */
  topics: string[];
  /** 1-30 */
  yearsOfExperience: number;
  /** 1-5 */
  maxMentees: number;
  /** Current active mentee count */
  activeMenteeCount: number;
  availability: AvailabilityMap;
  sessionFormats: SessionFormat[];
  /** 2-100 chars */
  professionalTitle: string;
  /** 2-100 chars */
  companyName: string;
  /** 100-1000 chars */
  bio: string;
  onboardingStatus: OnboardingStatus;
  /** Computed: activeMenteeCount < maxMentees */
  isAvailable: boolean;
  /** ISO 8601 timestamp */
  createdAt: string;
  /** ISO 8601 timestamp */
  updatedAt: string;
}

// --- Mentees Table ---

export interface MenteeEntity {
  /** Partition key - UUID */
  menteeId: string;
  /** GSI-UserId partition key - FK to Users table */
  userId: string;
  /** Max 10 items */
  skills: string[];
  experienceLevel: ExperienceLevel;
  /** 0-50 */
  yearsOfExperience: number;
  primaryGoal: PrimaryGoal;
  /** 50-500 chars */
  goalDescription: string;
  preferredDuration: PreferredDuration;
  availability: AvailabilityMap;
  communicationPreference: CommunicationPreference;
  /** S3 key (optional) */
  resumeUrl: string | null;
  onboardingStatus: OnboardingStatus;
  /** Active lock ID, null if none */
  currentLockId: string | null;
  /** ISO 8601 timestamp */
  createdAt: string;
  /** ISO 8601 timestamp */
  updatedAt: string;
}

// --- Sessions Table ---

export interface ChecklistState {
  prework: boolean[];
  followup: boolean[];
}

export interface AgendaItem {
  title: string;
  /** Minimum 3 minutes */
  durationMinutes: number;
  /** Max 500 chars */
  description: string;
}

export interface SessionPlan {
  /** Max 100 chars */
  sessionTitle: string;
  /** 3-7 items, durations sum to 35 minutes */
  agenda: AgendaItem[];
  /** 2-5 items, each max 200 chars */
  preworkTasks: string[];
  /** 2-5 items, each max 200 chars */
  followUpTasks: string[];
}

export interface SessionEntity {
  /** Partition key - UUID */
  sessionId: string;
  /** GSI-Mentee partition key - FK to Mentees table */
  menteeId: string;
  /** GSI-Mentor partition key - FK to Mentors table */
  mentorId: string;
  status: SessionStatus;
  /** JSON structure, max 50KB */
  sessionPlan: SessionPlan | null;
  /** ISO 8601 timestamp, null until mentee marks complete */
  menteeCompletedAt: string | null;
  /** ISO 8601 timestamp, null until mentor confirms */
  mentorCompletedAt: string | null;
  checklistState: ChecklistState | null;
  /** Associated lock ID */
  lockId: string | null;
  /** ISO 8601 timestamp */
  lockExpiresAt: string | null;
  /** 0-3, for background retry tracking */
  planRetryCount: number;
  /** ISO 8601 timestamp (GSI-Mentee-SK, GSI-Mentor-SK) */
  createdAt: string;
  /** ISO 8601 timestamp */
  updatedAt: string;
}

// --- Notifications Table ---

export interface NotificationEntity {
  /** Partition key - UUID */
  notificationId: string;
  /** GSI-Recipient partition key - target user */
  recipientUserId: string;
  type: NotificationType;
  /** Max 500 chars */
  message: string;
  /** FK to Sessions table (optional) */
  relatedSessionId: string | null;
  /** Default false */
  isRead: boolean;
  /** ISO 8601 timestamp (GSI-Recipient-SK) */
  createdAt: string;
}
