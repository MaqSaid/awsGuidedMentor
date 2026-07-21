---
inclusion: fileMatch
fileMatchPattern: "**/GuidedMentor.LocalDev/**"
---

# Local Development Patterns

## Dev Authentication
- Magic links are the ONLY auth mechanism (no passwords stored)
- In local dev, the magic link URL is logged to console: `[DEV] Magic link: http://localhost:3000/auth/verify?email=...&token=...`
- Email sending fails gracefully — token is always saved to PostgreSQL regardless
- Tokens: UUID, 10-minute TTL, single-use, stored in `auth_tokens` table
- DevAuthHandler accepts any Bearer token and extracts user ID from JWT payload

## Service Registration
- All DI registrations live in `Program.cs` (composition root)
- Mock services for external dependencies: `MockChatClient`, `MockIntentClassifier`, `MockNotificationPublisher`, `MockEventBridgePublisher`, `MockAnalyticsRepository`
- Real PostgreSQL repositories for all domain data
- `ValidateOnBuild = false` to avoid crashing on optional handler dependencies
- Hangfire connection string uses ADO.NET format: `Server=localhost;Port=5432;...` (not Npgsql `Host=` format)

## Running Locally
- PostgreSQL via Docker: `docker compose up -d postgres`
- Seed data: `docker exec -i guidedmentor-postgres psql -U dev -d guidedmentor < scripts/seed-demo-data.sql`
- Backend: `$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run --project src/Shared/GuidedMentor.LocalDev`
- Frontend: `cd frontend/host-shell && npm run dev`
- Backend port: 5000 (Development), 8080 (Production — uses PORT env var)

## Frontend Modes
- MSW mocks active by default (no `.env.local`)
- Real API mode: create `frontend/host-shell/.env.local` with `VITE_DISABLE_MOCKS=true`
- Frontend always reads `VITE_API_URL` for base URL

## Common Issues
- `Math.Abs(int.MinValue)` overflow: always guard with `hash == int.MinValue ? 0 : Math.Abs(hash)`
- Hangfire `UseNpgsqlConnection` requires ADO.NET format, not Npgsql key-value format
- `RecurringJob.AddOrUpdate` static API doesn't work — use `IRecurringJobManager` service
