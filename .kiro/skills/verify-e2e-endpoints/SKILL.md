# Verify E2E Endpoints

Systematically tests all API endpoints with real PostgreSQL data to confirm the full stack works without mocks.

## When to use
- After adding new endpoints or changing existing ones
- Before deploying to production
- After completing integration wiring tasks
- When switching from MSW mocks to real backend

## Prerequisites
- Docker running with `guidedmentor-postgres` container healthy
- Backend running: `$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run --project src/Shared/GuidedMentor.LocalDev`
- Frontend `.env.local` has `VITE_DISABLE_MOCKS=true`
- Seed data loaded: `docker exec -i guidedmentor-postgres psql -U dev -d guidedmentor < scripts/seed-demo-data.sql`

## Steps

1. **Verify health**: `GET /v1/health` → expect `{"status":"healthy"}`
2. **Auth flow**:
   - `POST /v1/auth/magic-link` with `{"email":"sarah.chen@example.com"}`
   - Get token from DB: `SELECT token FROM auth_tokens WHERE used=false ORDER BY created_at DESC LIMIT 1`
   - `POST /v1/auth/verify-magic-link` with `{"email":"...","token":"..."}` → expect JWT
3. **Mentee journey** (use JWT from step 2):
   - `GET /v1/dashboard/mentee`
   - `GET /v1/mentors` → expect 5 seeded mentors
   - `GET /v1/mentors/{mentorId}` → expect full profile
   - `GET /v1/sessions/{sessionId}/plan` → expect seeded plan
4. **Mentor journey**:
   - `GET /v1/dashboard/mentor`
   - `POST /v1/role/toggle` → switch to mentor
5. **Engagement**:
   - `GET /v1/notifications`
   - `GET /v1/opportunities`
   - `GET /v1/meetups`
   - `POST /v1/assistant/chat` with `{"message":"help","history":[]}`
6. **Onboarding**: `POST /v1/onboarding/step` with `{"role":"mentee","step":1,"data":{"displayName":"Test"}}`
7. **Session complete**: `POST /v1/sessions/{id}/complete` with `{"role":"mentee"}`

## Correct API Paths (Common Mistakes)
| Action | Correct Path | Wrong Path |
|--------|-------------|------------|
| Toggle role | `/v1/role/toggle` | `/v1/users/toggle-role` |
| Onboarding | `/v1/onboarding/step` | `/v1/onboarding/mentee` |
| AI Help | `/v1/assistant/chat` | `/v1/help/chat` |
| Session complete | `/v1/sessions/{id}/complete` (body: `{"role":"mentee"}`) | No body |

## Checklist
- [ ] Health endpoint returns 200
- [ ] Magic link creates token in DB
- [ ] Token verify returns JWT
- [ ] Mentee dashboard returns real data
- [ ] Browse mentors returns seeded mentors
- [ ] Session plan returns seeded JSONB data
- [ ] Role toggle succeeds
- [ ] Opportunities returns seeded postings
- [ ] Meetups returns upcoming events
- [ ] AI Help streams response
- [ ] Onboarding step saves successfully
