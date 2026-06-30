/**
 * API request and response interfaces for all GuidedMentor endpoints.
 */

import {
  AvailabilityMap,
  CommunicationPreference,
  ExperienceLevel,
  NotificationType,
  PreferredDuration,
  PrimaryGoal,
  SessionFormat,
  UserRole,
} from './enums';
import {
  ChecklistState,
  SessionPlan,
} from './entities';

// =============================================================================
// Auth API
// =============================================================================

export interface EmailSignupRequest {
  email: string;
  /** Min 8 chars, 1 upper, 1 lower, 1 number, 1 special */
  password: string;
}

export interface EmailSignupResponse {
  message: string;
  userId: string;
}

export interface GoogleOAuthRequest {
  /** Authorization code from Google */
  code: string;
}

export interface VerifyEmailRequest {
  email: string;
  /** 6-digit code, expires in 10 minutes */
  code: string;
}

export interface VerifyEmailResponse {
  message: string;
  verified: boolean;
}

export interface SignInRequest {
  email: string;
  password: string;
}

export interface SignInResponse {
  accessToken: string;
  refreshToken: string;
  idToken: string;
  role: UserRole;
  expiresIn: number;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  expiresIn: number;
}

export interface SignOutRequest {
  accessToken: string;
}

// =============================================================================
// Role Selection API
// =============================================================================

export interface SetRoleRequest {
  role: 'mentor' | 'mentee';
}

export interface SetRoleResponse {
  message: string;
  role: 'mentor' | 'mentee';
}

// =============================================================================
// Onboarding API
// =============================================================================

export interface MenteeStep1Data {
  fullName: string;
  profilePhotoUrl: string;
  awsChapter: string;
  location: string;
}

export interface MenteeStep2Data {
  skills: string[];
  experienceLevel: ExperienceLevel;
  yearsOfExperience: number;
}

export interface MenteeStep3Data {
  primaryGoal: PrimaryGoal;
  goalDescription: string;
  preferredDuration: PreferredDuration;
}

export interface MenteeStep4Data {
  availability: AvailabilityMap;
  communicationPreference: CommunicationPreference;
  resumeUrl?: string;
}

export type MenteeStepData =
  | MenteeStep1Data
  | MenteeStep2Data
  | MenteeStep3Data
  | MenteeStep4Data;

export interface MentorStep1Data {
  fullName: string;
  profilePhotoUrl: string;
  awsChapter: string;
  professionalTitle: string;
  companyName: string;
}

export interface MentorStep2Data {
  expertiseAreas: string[];
  yearsOfExperience: number;
  certifications: string[];
  topics: string[];
}

export interface MentorStep3Data {
  maxMentees: number;
  availability: AvailabilityMap;
  sessionFormats: SessionFormat[];
  bio: string;
}

export type MentorStepData =
  | MentorStep1Data
  | MentorStep2Data
  | MentorStep3Data;

export interface SaveOnboardingStepRequest {
  userId: string;
  role: 'mentor' | 'mentee';
  /** 1-4 for mentee, 1-3 for mentor */
  step: number;
  data: MenteeStepData | MentorStepData;
}

export interface SaveOnboardingStepResponse {
  message: string;
  completedStep: number;
  isComplete: boolean;
}

export interface GetOnboardingProgressResponse {
  currentStep: number;
  totalSteps: number;
  isComplete: boolean;
  savedData: Record<number, MenteeStepData | MentorStepData>;
}

// =============================================================================
// Matching & Browse API
// =============================================================================

export interface ScoreDimensions {
  /** 0-30 */
  chapterScore: number;
  /** 0-30 */
  skillsOverlap: number;
  /** 0-25 */
  goalAlignment: number;
  /** 0-15 */
  experienceGap: number;
}

export interface MentorBrowseCard {
  mentorId: string;
  displayName: string;
  profilePhotoUrl: string;
  professionalTitle: string;
  expertiseAreas: string[];
  availability: AvailabilityMap;
  compatibilityScore: number;
}

export interface MentorScore {
  mentorId: string;
  /** 0-100 */
  totalScore: number;
  dimensions: ScoreDimensions;
  mentorProfile: MentorBrowseCard;
}

export interface BrowseMentorsRequest {
  menteeId: string;
  page?: number;
  pageSize?: number;
}

export interface BrowseMentorsResponse {
  mentors: MentorScore[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// =============================================================================
// Locking API
// =============================================================================

export interface AcquireLockRequest {
  menteeId: string;
  mentorId: string;
}

export interface AcquireLockResponse {
  lockId: string;
  /** ISO 8601, 15 min from now */
  expiresAt: string;
  success: boolean;
  message?: string;
}

export interface ReleaseLockRequest {
  lockId: string;
  menteeId: string;
}

export interface ReleaseLockResponse {
  success: boolean;
  message: string;
}

export interface ConfirmSelectionRequest {
  lockId: string;
  menteeId: string;
  mentorId: string;
}

export interface ConfirmSelectionResponse {
  sessionId: string;
  message: string;
}

// =============================================================================
// Session API
// =============================================================================

export interface AcceptSessionRequest {
  sessionId: string;
  mentorId: string;
}

export interface AcceptSessionResponse {
  message: string;
  status: 'active';
}

export interface DeclineSessionRequest {
  sessionId: string;
  mentorId: string;
}

export interface DeclineSessionResponse {
  message: string;
}

export interface GetSessionPlanResponse {
  sessionId: string;
  status: string;
  sessionPlan: SessionPlan | null;
  checklistState: ChecklistState | null;
}

export interface UpdateChecklistRequest {
  sessionId: string;
  type: 'prework' | 'followup';
  index: number;
  checked: boolean;
}

export interface UpdateChecklistResponse {
  success: boolean;
  checklistState: ChecklistState;
}

export interface MarkCompleteRequest {
  sessionId: string;
  userId: string;
  role: 'mentor' | 'mentee';
}

export interface MarkCompleteResponse {
  message: string;
  status: string;
}

// =============================================================================
// Notification API
// =============================================================================

export interface NotificationItem {
  notificationId: string;
  type: NotificationType;
  message: string;
  relatedSessionId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface ListNotificationsResponse {
  notifications: NotificationItem[];
  totalCount: number;
}

export interface MarkNotificationReadRequest {
  notificationId: string;
}

export interface UnreadCountResponse {
  /** Exact count for 1-99, display "99+" for >99 */
  count: number;
  display: string;
}

// =============================================================================
// Dashboard API
// =============================================================================

export interface ActiveSessionCard {
  sessionId: string;
  mentorName: string;
  mentorId: string;
  sessionTitle: string;
  nextIncompleteFollowUp: string | null;
  progressPercentage: number;
}

export interface ProgressStats {
  completedSessions: number;
  inProgressSessions: number;
  pendingRequests: number;
  totalChecklistItemsCompleted: number;
  completionPercentage: number;
}

export interface MenteeDashboardResponse {
  activeSessions: ActiveSessionCard[];
  recommendedMentors: MentorScore[];
  progressStats: ProgressStats;
  hasActiveSessions: boolean;
}

export interface PendingRequest {
  sessionId: string;
  menteeId: string;
  menteeName: string;
  menteeProfilePhotoUrl: string;
  menteeGoal: PrimaryGoal;
  compatibilityScore: number;
  requestedAt: string;
}

export interface ActiveMenteeCard {
  sessionId: string;
  menteeId: string;
  menteeName: string;
  sessionTitle: string;
  status: string;
}

export interface ScheduleItem {
  sessionId: string;
  menteeName: string;
  date: string;
  timeSlot: string;
}

export interface MentorDashboardResponse {
  pendingRequests: PendingRequest[];
  activeMentees: ActiveMenteeCard[];
  upcomingSchedule: ScheduleItem[];
  activeMenteeCount: number;
  maxMentees: number;
}

// =============================================================================
// Upload API
// =============================================================================

export interface GetUploadUrlRequest {
  fileName: string;
  fileType: 'application/pdf' | 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
  fileSize: number;
}

export interface GetUploadUrlResponse {
  uploadUrl: string;
  /** ISO 8601, 5 min expiry */
  expiresAt: string;
  fileKey: string;
}

export interface GetDownloadUrlResponse {
  downloadUrl: string;
  /** ISO 8601, 15 min expiry */
  expiresAt: string;
}

// =============================================================================
// Settings API
// =============================================================================

export interface UpdateProfileRequest {
  /** Fields to update - same validation as onboarding */
  [key: string]: unknown;
}

export interface UpdateProfileResponse {
  message: string;
  updatedFields: string[];
}

// =============================================================================
// Error Responses
// =============================================================================

export interface ApiErrorResponse {
  statusCode: number;
  error: string;
  message: string;
  correlationId: string;
  fieldErrors?: Record<string, string>;
}
