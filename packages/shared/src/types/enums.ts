/**
 * Shared enums and union types for the GuidedMentor platform.
 */

// --- User & Role ---

export type UserRole = 'mentor' | 'mentee' | null;

export type OnboardingStatus = 'not_started' | 'in_progress' | 'completed';

// --- Experience & Skills ---

export type ExperienceLevel = 'beginner' | 'intermediate' | 'advanced';

// --- Mentee Goals ---

export type PrimaryGoal =
  | 'career_transition'
  | 'skill_development'
  | 'certification_preparation'
  | 'project_guidance';

export type PreferredDuration = '4_weeks' | '8_weeks' | '12_weeks';

export type CommunicationPreference = 'video_call' | 'voice_call' | 'chat';

// --- Sessions ---

export type SessionStatus =
  | 'pending'
  | 'active'
  | 'completed'
  | 'unresolved'
  | 'pending_plan'
  | 'plan_failed';

export type CompletionState =
  | 'active'
  | 'mentee_completed'
  | 'completed'
  | 'unresolved';

// --- Notifications ---

export type NotificationType =
  | 'request_sent'
  | 'request_accepted'
  | 'request_declined'
  | 'session_plan_ready'
  | 'completion_marked'
  | 'reminder';

// --- Availability ---

export type DayOfWeek =
  | 'monday'
  | 'tuesday'
  | 'wednesday'
  | 'thursday'
  | 'friday'
  | 'saturday'
  | 'sunday';

export interface TimeSlot {
  /** HH:MM format (24-hour) */
  start: string;
  /** HH:MM format (24-hour) */
  end: string;
}

export type AvailabilityMap = Record<DayOfWeek, TimeSlot[]>;

// --- Session Formats ---

export type SessionFormat = 'video_call' | 'voice_call' | 'chat';
