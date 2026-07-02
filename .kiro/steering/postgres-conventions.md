---
inclusion: fileMatch
fileMatchPattern: "**/Repositories/**"
---

# PostgreSQL & EF Core Conventions

## Repository Pattern
- Interface in Application layer (`I{Entity}Repository`)
- Implementation in Infrastructure layer (`Postgres{Entity}Repository`)
- Inject `GuidedMentorDbContext` via constructor
- Return Domain entities from public methods (map internally)
- EF entities (persistence models) live in `SharedInfrastructure/Data/Entities/`

## EF Core Patterns
- Use `DbContext.SaveChangesAsync()` — not raw SQL (except for complex queries)
- Use `FindAsync()` for primary key lookups
- Use `Include()` for eager loading relationships
- NEVER expose `IQueryable` from repositories
- Return `null` for not-found (not exceptions)

## Column Mapping
- Table names: lowercase plural (`users`, `mentors`, `sessions`)
- Column names: snake_case (`created_at`, `active_role`)
- Use `JSONB` for complex nested data (availability schedules, session plans)
- Use `TEXT[]` for string lists (skills, certifications, topics)
- Use `UUID` for all primary keys (auto-generated)
- Always include `created_at` and `updated_at` timestamps

## Connection String
- Local: `Host=localhost;Port=5432;Database=guidedmentor;Username=dev;Password=dev`
- Production: Supabase PostgreSQL URL (from environment variable)

## Transactions
- EF Core wraps `SaveChangesAsync()` in a transaction automatically
- For multi-entity operations: use explicit `BeginTransactionAsync()`

## Testing
- Integration tests use real PostgreSQL (Docker test container)
- Unit tests mock `GuidedMentorDbContext` via in-memory provider or NSubstitute
