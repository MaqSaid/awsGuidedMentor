/**
 * Content Context — API layer using TanStack Query
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { SessionPlanResponse, ChecklistUpdate } from '../types';

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

// ─── Session Plan ──────────────────────────────────────────────────────────────

export function useSessionPlan(sessionId: string) {
  return useQuery<SessionPlanResponse>({
    queryKey: ['session-plan', sessionId],
    queryFn: () => apiFetch(`/sessions/${sessionId}/plan`),
    enabled: !!sessionId,
  });
}

// ─── Checklist Toggle ──────────────────────────────────────────────────────────

export function useUpdateChecklist(sessionId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, Error, ChecklistUpdate>({
    mutationFn: (update) =>
      apiFetch(`/sessions/${sessionId}/checklist`, {
        method: 'PUT',
        body: JSON.stringify(update),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session-plan', sessionId] });
    },
  });
}
