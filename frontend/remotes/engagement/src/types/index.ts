/**
 * Shared types for the Engagement context frontend.
 */

// --- Dashboard Types ---

export interface SessionCard {
  sessionId: string;
  mentorName: string;
  menteeName: string;
  sessionTitle: string;
  status: 'pending_acceptance' | 'pending_plan' | 'active' | 'mentee_completed' | 'completed' | 'unresolved';
  progressPercent: number;
  nextTask?: string;
  createdAt: string;
}

export interface MentorRecommendation {
  mentorId: string;
  displayName: string;
  profilePhotoUrl?: string;
  chapter: string;
  professionalTitle: string;
  compatibilityScore: number;
  expertiseAreas: string[];
}

export interface ProgressStats {
  completedSessions: number;
  inProgressSessions: number;
  pendingRequests: number;
  totalChecklistCompleted: number;
  overallCompletionPercent: number;
}

export interface MenteeDashboardData {
  activeSessions: SessionCard[];
  topMentors: MentorRecommendation[];
  progressStats: ProgressStats;
  upcomingMeetups: MeetupEvent[];
}

export interface PendingRequest {
  sessionId: string;
  menteeName: string;
  menteeGoal: string;
  compatibilityScore: number;
  requestedAt: string;
}

export interface ActiveMentee {
  sessionId: string;
  menteeName: string;
  sessionTitle: string;
  progressPercent: number;
  status: string;
}

export interface MentorDashboardData {
  pendingRequests: PendingRequest[];
  activeMentees: ActiveMentee[];
  activeMenteeCount: number;
  maxMentees: number;
  isAvailable: boolean;
  upcomingMeetups: MeetupEvent[];
}

// --- Notification Types ---

export type NotificationType =
  | 'request_sent'
  | 'request_accepted'
  | 'request_declined'
  | 'session_plan_ready'
  | 'completion_marked'
  | 'reminder';

export interface Notification {
  notificationId: string;
  type: NotificationType;
  message: string;
  actionUrl: string;
  isRead: boolean;
  createdAt: string;
}

// --- AI Help Types ---

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

// --- Onboarding Tour Types ---

export interface TourStep {
  id: string;
  target: string;
  title: string;
  content: string;
  placement: 'top' | 'bottom' | 'left' | 'right';
}

// --- Meetup Types ---

export interface MeetupEvent {
  meetupId: string;
  title: string;
  chapter: string;
  date: string;
  startTime: string;
  endTime: string;
  venueName: string;
  venueAddress: string;
  eventUrl: string;
  confirmedAttendees: number;
}

// --- Analytics Types ---

export interface AnalyticsMetrics {
  dau: number;
  wau: number;
  mau: number;
  dauHistory: { date: string; count: number }[];
  wauHistory: { date: string; count: number }[];
  mauHistory: { date: string; count: number }[];
}

export interface FeatureHeatmapItem {
  feature: string;
  usageCount: number;
  percentage: number;
}

export interface FunnelStep {
  step: string;
  count: number;
  dropoff: number;
}

export interface ErrorHotspot {
  page: string;
  errorCount: number;
  errorRate: number;
}
