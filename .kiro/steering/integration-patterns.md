---
inclusion: fileMatch
fileMatchPattern: "**/host-shell/src/**"
---

# Frontend-Backend Integration Patterns

## API Path Reference

All frontend API calls must use these exact paths:

| Feature | Method | Path | Body |
|---------|--------|------|------|
| Health | GET | `/v1/health` | — |
| Magic link request | POST | `/v1/auth/magic-link` | `{"email":"..."}` |
| Magic link verify | POST | `/v1/auth/verify-magic-link` | `{"email":"...","token":"..."}` |
| Mentee dashboard | GET | `/v1/dashboard/mentee` | — |
| Mentor dashboard | GET | `/v1/dashboard/mentor` | — |
| Browse mentors | GET | `/v1/mentors` | — |
| Mentor detail | GET | `/v1/mentors/{mentorId}` | — |
| Session plan | GET | `/v1/sessions/{sessionId}/plan` | — |
| Role toggle | POST | `/v1/role/toggle` | — |
| Role select | POST | `/v1/role/select` | `{"role":"Mentor"}` |
| Notifications | GET | `/v1/notifications` | — |
| Unread count | GET | `/v1/notifications/count` | — |
| Opportunities | GET | `/v1/opportunities` | — |
| Meetups | GET | `/v1/meetups` | — |
| AI Help | POST | `/v1/assistant/chat` | `{"message":"...","history":[]}` |
| Onboarding step | POST | `/v1/onboarding/step` | `{"role":"mentee","step":1,"data":{...}}` |
| Onboarding progress | GET | `/v1/onboarding/progress?role=mentee` | — |
| Session complete | POST | `/v1/sessions/{id}/complete` | `{"role":"mentee"}` |
| Locking | POST | `/v1/locking/acquire` | `{"mentorId":"..."}` |

## Auth Header
All authenticated endpoints require:
```
Authorization: Bearer {jwt_token}
```

The JWT is obtained from the verify-magic-link response and stored in `localStorage` as `gm_access_token`.

## Response Shapes

### Mentors list
```json
{
  "mentors": [...],
  "totalCount": 5,
  "page": 1,
  "pageSize": 12
}
```

### Session plan
```json
{
  "sessionId": "...",
  "sessionTitle": "...",
  "agenda": [{"title":"...","durationMinutes":5,"description":"..."}],
  "preworkTasks": ["..."],
  "followUpTasks": ["..."]
}
```

### Auth verify response
```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "idToken": "...",
  "activeRole": "mentor|mentee",
  "expiresIn": 900
}
```

## MSW vs Real API
- Local dev with MSW: `VITE_DISABLE_MOCKS` absent or not `"true"` — MSW intercepts all requests
- Local dev without MSW: `VITE_DISABLE_MOCKS=true` in `.env.local` — real API at `VITE_API_URL`
- Production: `VITE_DISABLE_MOCKS=true` always set in Cloudflare Pages env vars

## Common Integration Issues
- Frontend sends `Content-Type: application/json` on POST — backend requires it for body parsing
- Empty POST body triggers "no body provided" error — always send at least `{}`
- Role toggle returns the new role enum value (not string) — frontend maps it
- Streaming responses (AI Help) use Server-Sent Events (`text/event-stream`)
