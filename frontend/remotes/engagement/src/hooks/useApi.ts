/**
 * API hooks for the Engagement context using TanStack Query.
 *
 * Requirements: 10.1, 10.3, 11.1, 11.2, 12.3, 12.5, 29.9, 30.6
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type {
  MenteeDashboardData,
  MentorDashboardData,
  Notification,
  MeetupEvent,
  AnalyticsMetrics,
  FeatureHeatmapItem,
  FunnelStep,
  ErrorHotspot,
} from '../types';

const API_BASE = '/v1';

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) {
    throw new Error(`API Error: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

// --- Mentee Dashboard ---
export function useMenteeDashboard() {
  return useQuery<MenteeDashboardData>({
    queryKey: ['menteeDashboard'],
    queryFn: () => apiFetch('/dashboard/mentee'),
    staleTime: 30_000,
  });
}

// --- Mentor Dashboard ---
export function useMentorDashboard() {
  return useQuery<MentorDashboardData>({
    queryKey: ['mentorDashboard'],
    queryFn: () => apiFetch('/dashboard/mentor'),
    staleTime: 30_000,
  });
}

export function useAcceptRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sessionId: string) =>
      apiFetch(`/sessions/${sessionId}/accept`, { method: 'POST' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentorDashboard'] });
    },
  });
}

export function useDeclineRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sessionId: string) =>
      apiFetch(`/sessions/${sessionId}/decline`, { method: 'POST' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentorDashboard'] });
    },
  });
}

export function useToggleAvailability() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (isAvailable: boolean) =>
      apiFetch('/users/me', {
        method: 'PUT',
        body: JSON.stringify({ availabilityStatus: isAvailable ? 'available' : 'unavailable' }),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mentorDashboard'] });
    },
  });
}

// --- Notifications ---
export function useNotifications() {
  return useQuery<Notification[]>({
    queryKey: ['notifications'],
    queryFn: () => apiFetch('/notifications'),
    staleTime: 10_000,
  });
}

export function useUnreadCount() {
  return useQuery<{ count: number }>({
    queryKey: ['notificationsUnreadCount'],
    queryFn: () => apiFetch('/notifications/count'),
    staleTime: 10_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (notificationId: string) =>
      apiFetch(`/notifications/${notificationId}/read`, { method: 'PUT' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
    },
  });
}

export function useMarkAllRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiFetch('/notifications/read-all', { method: 'PUT' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsUnreadCount'] });
    },
  });
}

// --- Meetups ---
export function useUpcomingMeetups(chapter?: string) {
  return useQuery<MeetupEvent[]>({
    queryKey: ['meetups', chapter],
    queryFn: () => apiFetch(`/meetups?chapter=${chapter ?? ''}`),
    staleTime: 60_000,
  });
}

// --- Analytics (Admin) ---
export function useAnalyticsMetrics() {
  return useQuery<AnalyticsMetrics>({
    queryKey: ['analyticsMetrics'],
    queryFn: () => apiFetch('/admin/analytics/metrics'),
    staleTime: 60_000,
  });
}

export function useFeatureHeatmap() {
  return useQuery<FeatureHeatmapItem[]>({
    queryKey: ['featureHeatmap'],
    queryFn: () => apiFetch('/admin/analytics/heatmap'),
    staleTime: 60_000,
  });
}

export function useFunnels() {
  return useQuery<FunnelStep[]>({
    queryKey: ['funnels'],
    queryFn: () => apiFetch('/admin/analytics/funnels'),
    staleTime: 60_000,
  });
}

export function useErrorHotspots() {
  return useQuery<ErrorHotspot[]>({
    queryKey: ['errorHotspots'],
    queryFn: () => apiFetch('/admin/analytics/errors'),
    staleTime: 60_000,
  });
}

// --- Tour ---
export function useTourStatus() {
  return useQuery<{ dismissed: boolean }>({
    queryKey: ['tourStatus'],
    queryFn: () => apiFetch('/tour/status'),
    staleTime: 300_000,
  });
}

export function useDismissTour() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiFetch('/tour/dismiss', { method: 'PUT' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tourStatus'] });
    },
  });
}
