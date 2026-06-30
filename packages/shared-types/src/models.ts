import type {
  Role,
  ExperienceLevel,
  PrimaryGoal,
  PreferredDuration,
  CommunicationPreference,
  SessionStatus,
  NotificationType,
  OnboardingStatus,
  DayOfWeek,
} from './enums';

/** Time slot for availability */
export interface TimeSlot {
  start: string; // HH:MM format
  end: string;   // HH:MM format
}

/** Weekly availability map */
export type AvailabilityMap = Partial<Record<DayOfWeek, TimeSlot[]>>;

/** Users table entity */
export interface UserRecord {
  userId: string;
  email: string;
  role: Role | null;
  displayName: string;
  profilePhotoUrl: string;
  awsChapter: string;
  location: string;
  onboardingStatus: OnboardingStatus;
  failedLoginAttempts: number;
  lockedUntil: string | null;
  createdAt: string;
  updatedAt: string;
}

/** Mentors table entity */
export interface MentorProfile {
  mentorId: string;
  userId: string;
  expertiseAreas: string[];    // 1-10 items
  certifications: string[];    // 0-15 items
  topics: string[];            // 1-10 items
  yearsOfExperience: number;   // 1-30
  maxMentees: number;          // 1-5
  activeMenteeCount: number;
  availability: AvailabilityMap;
  sessionFormats: CommunicationPreference[];
  professionalTitle: string;   // 2-100 chars
  companyName: string;         // 2-100 chars
  bio: string;                 // 100-1000 chars
  onboardingStatus: OnboardingStatus;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

/** Mentees table entity */
export interface MenteeProfile {
  menteeId: string;
  userId: string;
  skills: string[];            // 1-10 items
  experienceLevel: ExperienceLevel;
  yearsOfExperience: number;   // 0-50
  primaryGoal: PrimaryGoal;
  goalDescription: string;     // 50-500 chars
  preferredDuration: PreferredDuration;
  availability: AvailabilityMap;
  communicationPreference: CommunicationPreference;
  resumeUrl?: string;
  onboardingStatus: OnboardingStatus;
  currentLockId: string | null;
  createdAt: string;
  updatedAt: string;
}

/** Session plan agenda item */
export interface AgendaItem {
  title: string;
  durationMinutes: number;     // min 3 minutes
  description: string;         // max 500 chars
}

/** AI-generated session plan */
export interface SessionPlan {
  sessionTitle: string;        // max 100 chars
  agenda: AgendaItem[];        // 3-7 items, durations sum to 35 min
  preworkTasks: string[];      // 2-5 items, each max 200 chars
  followUpTasks: string[];     // 2-5 items, each max 200 chars
}

/** Sessions table entity */
export interface SessionRecord {
  sessionId: string;
  menteeId: string;
  mentorId: string;
  status: SessionStatus;
  sessionPlan: SessionPlan | null;
  menteeCompletedAt: string | null;
  mentorCompletedAt: string | null;
  checklistState: {
    prework: boolean[];
    followup: boolean[];
  };
  lockId: string;
  lockExpiresAt: string;
  planRetryCount: number;      // 0-3
  createdAt: string;
  updatedAt: string;
}

/** Notifications table entity */
export interface NotificationRecord {
  notificationId: string;
  recipientUserId: string;
  type: NotificationType;
  message: string;             // max 500 chars
  relatedSessionId?: string;
  isRead: boolean;
  createdAt: string;
}

/** Mentor browse card (displayed in browse page) */
export interface MentorBrowseCard {
  mentorId: string;
  displayName: string;
  profilePhotoUrl: string;
  professionalTitle: string;
  expertiseAreas: string[];
  availability: AvailabilityMap;
  compatibilityScore: number;
}

/** Matching score breakdown */
export interface MentorScore {
  mentorId: string;
  totalScore: number;          // 0-100
  dimensions: {
    chapterScore: number;      // 0-30
    skillsOverlap: number;     // 0-30
    goalAlignment: number;     // 0-25
    experienceGap: number;     // 0-15
  };
  mentorProfile: MentorBrowseCard;
}
