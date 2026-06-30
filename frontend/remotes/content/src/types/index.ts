/**
 * Content Context — Shared Types
 */

export interface AgendaItem {
  title: string;
  durationMinutes: number;
  description: string;
}

export interface SessionPlan {
  sessionTitle: string;
  agenda: AgendaItem[];
  preworkTasks: string[];
  followUpTasks: string[];
}

export interface ChecklistState {
  prework: boolean[];
  followup: boolean[];
}

export interface SessionPlanResponse {
  sessionId: string;
  status: SessionStatus;
  plan: SessionPlan | null;
  checklistState: ChecklistState;
}

export type SessionStatus =
  | 'pending_acceptance'
  | 'pending_plan'
  | 'active'
  | 'mentee_completed'
  | 'completed'
  | 'unresolved';

export interface ChecklistUpdate {
  type: 'prework' | 'followup';
  index: number;
  checked: boolean;
}
