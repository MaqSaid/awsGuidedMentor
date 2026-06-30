/**
 * Mentoring Context — Shared Types
 */

export type OpportunityType = 'job' | 'workshop' | 'event' | 'training';
export type EmploymentType = 'full-time' | 'part-time' | 'contract' | 'internship';
export type ExperienceLevel = 'beginner' | 'intermediate' | 'advanced' | 'any';
export type SessionStatus = 'pending_acceptance' | 'pending_plan' | 'active' | 'mentee_completed' | 'completed' | 'unresolved';
export type AvailabilityStatus = 'available' | 'unavailable';

export interface MentorBrowseResult {
  mentorId: string;
  userId: string;
  displayName: string;
  profilePhotoUrl?: string;
  professionalTitle: string;
  companyName: string;
  awsChapter: string;
  expertiseAreas: string[];
  topics: string[];
  certifications: string[];
  yearsOfExperience: number;
  maxMentees: number;
  activeMenteeCount: number;
  availability: Record<string, string[]>;
  sessionFormats: string[];
  bio: string;
  compatibilityScore: number;
  chapterScore: number;
  skillsOverlap: number;
  goalAlignment: number;
  experienceGap: number;
  hasActiveOpportunities: boolean;
  availabilityStatus: AvailabilityStatus;
  returnDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface SessionSummary {
  sessionId: string;
  mentorId: string;
  menteeId: string;
  mentorName: string;
  menteeName: string;
  status: SessionStatus;
  sessionTitle?: string;
  createdAt: string;
  updatedAt: string;
  menteeCompletedAt?: string;
  mentorCompletedAt?: string;
  checklistProgress?: number;
}

export interface OpportunityPosting {
  postingId: string;
  mentorId: string;
  mentorName: string;
  title: string;
  type: OpportunityType;
  organisationName: string;
  description: string;
  location: string;
  eventDateTime?: string;
  employmentType?: EmploymentType;
  requiredSkills: string[];
  experienceLevel: ExperienceLevel;
  externalUrl: string;
  publishedAt: string;
  expiresAt: string;
  isExpired: boolean;
  isActive: boolean;
  daysRemaining: number;
  isMentorMatch: boolean;
}

export interface CreateOpportunityDto {
  title: string;
  type: OpportunityType;
  organisationName: string;
  description: string;
  location: string;
  eventDateTime?: string;
  employmentType?: EmploymentType;
  requiredSkills: string[];
  experienceLevel: ExperienceLevel;
  externalUrl: string;
}

export interface OpportunityNotificationPreferences {
  enabledTypes: OpportunityType[];
  skillMatchEnabled: boolean;
}

export interface MentorAvailability {
  status: AvailabilityStatus;
  reason?: string;
  returnDate?: string;
}

export interface LockResult {
  lockId: string;
  mentorId: string;
  expiresAt: string;
}

export interface BrowseFilters {
  chapter?: string;
  skills?: string[];
  availableOnly?: boolean;
}

export interface OpportunityFilters {
  type?: OpportunityType;
  location?: string;
  skills?: string[];
  experienceLevel?: ExperienceLevel;
  employmentType?: EmploymentType;
}
