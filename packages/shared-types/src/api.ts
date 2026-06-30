import type { Role, CommunicationPreference } from './enums';
import type { SessionPlan, AvailabilityMap } from './models';

/** Email sign-up request */
export interface EmailSignupRequest {
  email: string;
  password: string;
}

/** Sign-in request */
export interface SignInRequest {
  email: string;
  password: string;
}

/** Sign-in response */
export interface SignInResponse {
  accessToken: string;
  refreshToken: string;
  idToken: string;
  role: Role | null;
  expiresIn: number;
}

/** Email verification request */
export interface VerifyEmailRequest {
  email: string;
  code: string;
}

/** Acquire lock request */
export interface AcquireLockRequest {
  menteeId: string;
  mentorId: string;
}

/** Acquire lock response */
export interface AcquireLockResponse {
  lockId: string;
  expiresAt: string;
  success: boolean;
  message?: string;
}

/** Generate session plan request */
export interface GenerateSessionPlanRequest {
  sessionId: string;
  menteeProfile: {
    skills: string[];
    experienceLevel: string;
    yearsOfExperience: number;
    primaryGoal: string;
    goalDescription: string;
  };
  mentorProfile: {
    expertiseAreas: string[];
    yearsOfExperience: number;
    certifications: string[];
    topics: string[];
  };
}

/** Mark session complete request */
export interface MarkCompleteRequest {
  sessionId: string;
  userId: string;
  role: Role;
}

/** API error response */
export interface ApiErrorResponse {
  statusCode: number;
  error: string;
  message: string;
  correlationId: string;
  fieldErrors?: Record<string, string>;
}
