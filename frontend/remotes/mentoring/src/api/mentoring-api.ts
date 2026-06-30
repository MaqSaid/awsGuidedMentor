/**
 * Mentoring Context — API layer using TanStack Query
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type {
  MentorBrowseResult,
  PagedResult,
  SessionSummary,
  OpportunityPosting,
  CreateOpportunityDto,
  OpportunityNotificationPreferences,
  MentorAvailability,
  LockResult,
  BrowseFilters,
  OpportunityFilters,
} from '../types';

const API_BASE = '/v1';

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Request failed' }));
    throw new Error(error.message || `HTTP ${res.status}`);
  }
  return res.json();
}

// ─── Browse Mentors ────────────────────────────────────────────────────────────

export function useBrowseMentors(page: number, filters: BrowseFilters) {
  return useQuery<PagedResult<MentorBrowseResult>>({
    queryKey: ['mentors', 'browse', page, filters],
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '12' });
      if (filters.chapter) params.set('chapter', filters.chapter);
      if (filters.skills?.length) params.set('skills', filters.skills.join(','));
      if (filters.availableOnly) params.set('availableOnly', 'true');
      return apiFetch(`/mentoring/browse?${params}`);
    },
  });
}

// ─── Locking ───────────────────────────────────────────────────────────────────

export function useAcquireLock() {
  const queryClient = useQueryClient();
  return useMutation<LockResult, Error, { mentorId: string }>({
    mutationFn: ({ mentorId }) =>
      apiFetch('/mentoring/lock', {
        method: 'POST',
        body: JSON.stringify({ mentorId }),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentors', 'browse'] });
    },
  });
}

export function useReleaseLock() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, { lockId: string }>({
    mutationFn: ({ lockId }) =>
      apiFetch(`/mentoring/lock/${lockId}`, { method: 'DELETE' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentors', 'browse'] });
    },
  });
}

export function useConfirmSelection() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, { lockId: string; mentorId: string }>({
    mutationFn: ({ lockId, mentorId }) =>
      apiFetch(`/mentoring/lock/${lockId}/confirm`, {
        method: 'POST',
        body: JSON.stringify({ mentorId }),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentors', 'browse'] });
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
    },
  });
}

// ─── Sessions ──────────────────────────────────────────────────────────────────

export function useSessions(status?: string) {
  return useQuery<SessionSummary[]>({
    queryKey: ['sessions', status],
    queryFn: () => {
      const params = status ? `?status=${status}` : '';
      return apiFetch(`/mentoring/sessions${params}`);
    },
  });
}

export function useCancelSession() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, string>({
    mutationFn: (sessionId) =>
      apiFetch(`/mentoring/sessions/${sessionId}/cancel`, { method: 'POST' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
    },
  });
}

// ─── Opportunities ─────────────────────────────────────────────────────────────

export function useOpportunities(page: number, filters: OpportunityFilters) {
  return useQuery<PagedResult<OpportunityPosting>>({
    queryKey: ['opportunities', page, filters],
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '12' });
      if (filters.type) params.set('type', filters.type);
      if (filters.location) params.set('location', filters.location);
      if (filters.skills?.length) params.set('skills', filters.skills.join(','));
      if (filters.experienceLevel) params.set('experienceLevel', filters.experienceLevel);
      if (filters.employmentType) params.set('employmentType', filters.employmentType);
      return apiFetch(`/opportunities?${params}`);
    },
  });
}

export function useMentorOpportunities() {
  return useQuery<OpportunityPosting[]>({
    queryKey: ['opportunities', 'mine'],
    queryFn: () => apiFetch('/opportunities/mine'),
  });
}

export function useCreateOpportunity() {
  const queryClient = useQueryClient();
  return useMutation<OpportunityPosting, Error, CreateOpportunityDto>({
    mutationFn: (dto) =>
      apiFetch('/opportunities', {
        method: 'POST',
        body: JSON.stringify(dto),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opportunities'] });
    },
  });
}

export function useUpdateOpportunity() {
  const queryClient = useQueryClient();
  return useMutation<OpportunityPosting, Error, { id: string; dto: CreateOpportunityDto }>({
    mutationFn: ({ id, dto }) =>
      apiFetch(`/opportunities/${id}`, {
        method: 'PUT',
        body: JSON.stringify(dto),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opportunities'] });
    },
  });
}

// ─── Opportunity Notification Preferences ──────────────────────────────────────

export function useOpportunityPreferences() {
  return useQuery<OpportunityNotificationPreferences>({
    queryKey: ['opportunity-preferences'],
    queryFn: () => apiFetch('/settings/opportunity-preferences'),
  });
}

export function useUpdateOpportunityPreferences() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, OpportunityNotificationPreferences>({
    mutationFn: (prefs) =>
      apiFetch('/settings/opportunity-preferences', {
        method: 'PUT',
        body: JSON.stringify(prefs),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opportunity-preferences'] });
    },
  });
}

// ─── Mentor Availability ───────────────────────────────────────────────────────

export function useMentorAvailability() {
  return useQuery<MentorAvailability>({
    queryKey: ['mentor-availability'],
    queryFn: () => apiFetch('/mentoring/availability'),
  });
}

export function useSetMentorAvailability() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, MentorAvailability>({
    mutationFn: (availability) =>
      apiFetch('/mentoring/availability', {
        method: 'PUT',
        body: JSON.stringify(availability),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentor-availability'] });
      queryClient.invalidateQueries({ queryKey: ['mentors', 'browse'] });
    },
  });
}
