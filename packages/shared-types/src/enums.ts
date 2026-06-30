/** User role in the system */
export type Role = 'mentor' | 'mentee';

/** Experience levels for mentees */
export type ExperienceLevel = 'beginner' | 'intermediate' | 'advanced';

/** Primary goals a mentee can select */
export type PrimaryGoal =
  | 'career_transition'
  | 'skill_development'
  | 'certification_preparation'
  | 'project_guidance';

/** Preferred mentorship duration */
export type PreferredDuration = '4_weeks' | '8_weeks' | '12_weeks';

/** Communication methods available */
export type CommunicationPreference = 'video_call' | 'voice_call' | 'chat';

/** Session status lifecycle */
export type SessionStatus =
  | 'pending'
  | 'active'
  | 'completed'
  | 'unresolved'
  | 'pending_plan'
  | 'plan_failed';

/** Notification event types */
export type NotificationType =
  | 'request_sent'
  | 'request_accepted'
  | 'request_declined'
  | 'session_plan_ready'
  | 'completion_marked'
  | 'reminder';

/** Onboarding status */
export type OnboardingStatus = 'not_started' | 'in_progress' | 'completed';

/** Days of the week */
export type DayOfWeek =
  | 'monday'
  | 'tuesday'
  | 'wednesday'
  | 'thursday'
  | 'friday'
  | 'saturday'
  | 'sunday';

/** Completion state machine states */
export type CompletionState =
  | 'active'
  | 'mentee_completed'
  | 'completed'
  | 'unresolved';
