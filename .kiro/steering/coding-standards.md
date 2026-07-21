---
inclusion: always
---

# GuidedMentor Coding Standards

## C# Backend Conventions

### General
- Target .NET 10 with Native AOT compilation
- Enable nullable reference types everywhere — no `#nullable disable`
- Use `sealed` on all classes unless explicitly designed for inheritance
- Prefer `record` types for DTOs, commands, queries, and value objects
- Use primary constructors for simple dependency injection
- File-scoped namespaces always (`namespace X;` not `namespace X { }`)

### Naming
- PascalCase for public members, types, methods, properties
- camelCase with underscore prefix for private fields (`_repository`)
- Suffix interfaces with the pattern: `I{Noun}Repository`, `I{Noun}Service`, `I{Noun}Publisher`
- Suffix MediatR commands: `{Verb}{Noun}Command` (e.g., `UpdateSettingsCommand`)
- Suffix MediatR queries: `Get{Noun}Query` (e.g., `GetMenteeDashboardQuery`)
- Suffix handlers: `{CommandName}Handler`
- Suffix validators: `{CommandName}Validator`

### AOT Compatibility
- Use `System.Text.Json` source generation (`[JsonSerializable]` contexts) in API projects
- Avoid `System.Reflection.Emit` and dynamic proxies
- Note: AOT is optional for the free stack (Railway runs standard .NET); keep AOT-ready patterns for future AWS migration

### Numeric Safety
- Never call `Math.Abs()` on unvalidated `GetHashCode()` results — `int.MinValue` causes `OverflowException`
- Pattern: `var hash = x.GetHashCode(); var safe = hash == int.MinValue ? 0 : Math.Abs(hash);`

### Background Jobs (Hangfire)
- Use `IRecurringJobManager` (service-based API) — never `RecurringJob.AddOrUpdate` (static API fails before storage init)
- Hangfire PostgreSQL requires ADO.NET connection format: `Server=host;Port=5432;Database=db;User Id=user;Password=pass`
- The EF Core Npgsql format (`Host=host;...`) is NOT compatible with Hangfire.PostgreSql

### Error Handling
- Return `Result` or `Result<T>` from handlers — never throw for business logic failures
- Reserve exceptions for infrastructure failures (network, PostgreSQL, external APIs)
- Use Polly v8 resilience pipelines for retries, not manual retry loops

### Authentication
- Passwordless only — magic link via self-issued JWT
- Never store passwords — users authenticate via magic link email or Google OAuth
- Magic link tokens: UUID, 10-minute TTL, single-use, stored in `auth_tokens` PostgreSQL table
- Rate limit: max 3 magic link requests per email per 15 minutes
- Always return 200 on magic link request (prevent email enumeration)
- Expired tokens cleaned by Hangfire job (every 5 minutes)

### PostgreSQL (EF Core)
- Use `GuidedMentorDbContext` for all data access
- EF entities are persistence models — separate from Domain entities
- Use `JSONB` columns for complex nested data
- Use `TEXT[]` columns for string arrays
- Repository methods return Domain entities (map internally)
- Use `SaveChangesAsync()` for atomic writes
- Explicit transactions for multi-entity operations
- Connection string from `ConnectionStrings:DefaultConnection`

### Background Jobs (Hangfire)
- One class per job in `SharedInfrastructure/Jobs/`
- Always log start/end with ILogger
- Handle partial failures gracefully
- Use cron expressions for scheduling
- Jobs registered in Program.cs startup using `IRecurringJobManager` (never static `RecurringJob.AddOrUpdate`)
- Inject `GuidedMentorDbContext` + `ILogger<T>` + notification publishers as needed
- Jobs: `CleanupExpiredTokensJob` (5min), `OpportunityExpiryJob` (daily), `LockExpiryJob` (5min), `SessionEscalationJob` (daily), `CompletionReminderJob` (daily)

### Endpoint Registration (LocalDev)
- Identity endpoints: `app.MapAuthEndpoints()` — handles auth, role, onboarding, settings, upload
- Mentoring endpoints: `app.MapBrowseEndpoints()`, `app.MapSessionEndpoints()`, `app.MapOpportunityEndpoints()`, etc.
- Engagement endpoints: inline in Program.cs (notifications, dashboards, meetups, AI assistant)
- When adding a new endpoint: always update `.kiro/steering/integration-patterns.md`
- POST endpoints must accept request body (even if empty `{}`) — ASP.NET rejects bodyless POST with 400/415

### Email (Gmail SMTP)
- Use `IEmailSender` interface for all email operations
- Implementation uses MailKit + Gmail SMTP (port 587, StartTLS)
- Configuration in `Email` section of appsettings
- Rate limit: 500 emails/day (Gmail personal account limit)
- Never log email passwords or app passwords

### Real-Time (SignalR)
- SignalR hub at `/hubs/notifications`
- Frontend connects via `@microsoft/signalr` client
- Send notifications via `IHubContext<NotificationHub>`
- Fallback: polling every 30 seconds if WebSocket fails

## React/TypeScript Frontend Conventions

### Components
- Functional components only — no class components
- Export named components (not default) from shared packages
- Use `export default` only for page-level route components
- Colocate types with their component file or in a `types/index.ts`

### Hooks
- Prefix custom hooks with `use` (e.g., `useDebounceValidation`)
- TanStack Query for all server state — no `useEffect` for data fetching
- Keep hooks in a `hooks/` directory per remote or package

### Styling
- TailwindCSS 4 utility classes — no inline styles except for dynamic values
- Use CSS custom properties (design tokens) via `var(--color-*)` for theming
- Glassmorphism: use the `.glass-card` utility class
- Respect `prefers-reduced-motion` — wrap animations in media query checks

### Accessibility (WCAG 2.1 AA)
- Every interactive element must be keyboard-accessible
- Use semantic HTML (`<nav>`, `<main>`, `<section>`, `<article>`)
- Include `aria-label` on all icon-only buttons
- Use `aria-live="polite"` for dynamic content updates (errors, loading, notifications)
- Every form field needs a visible `<label>` or `aria-label`
- Focus management: trap focus in modals, restore on close
- Skip-nav link as first focusable element on every page

### Module Federation
- Shared dependencies: react, react-dom, react-router-dom, @tanstack/react-query
- Each remote exposes page components and key shared components
- Use `ErrorBoundary` + `Suspense` when loading remote modules
- Design system package (`@guided-mentor/design-system`) is shared, not federated

## Architecture Rules

### Clean Architecture Layers
- **Domain** → zero dependencies on other layers; pure business logic, entities, value objects
- **Application** → depends only on Domain; contains commands, queries, handlers, interfaces
- **Infrastructure** → implements Application interfaces; EF Core, PostgreSQL, SignalR, Hangfire, MailKit
- **Api** → thin HTTP layer; maps requests to MediatR commands/queries

### Domain-Driven Design
- Each bounded context (Identity, Mentoring, Content, Engagement) is independent
- Cross-context communication via interfaces (anti-corruption layer) — never direct references
- Domain events for cross-context side effects (via MediatR notifications + SignalR)
- Aggregate roots enforce invariants; entities are only modified through their aggregate

### MediatR Pipeline
- Order: ValidationBehavior → LoggingBehavior → AuditLoggingBehavior → PerformanceBehavior → Handler
- Commands implement `IAuditableCommand` for audit logging
- Admin commands implement `IAdminCommand` for enhanced audit trail
- FluentValidation validators are auto-discovered per assembly

### Repository Pattern
- Interface in Application layer, implementation in Infrastructure
- One repository per aggregate root
- Repository methods: `GetByIdAsync`, `SaveAsync`, `DeleteAsync`
- Return domain entities from repositories, never DTOs

## Testing Standards

### Test Naming
- Format: `{Method}_{Scenario}_{ExpectedResult}` (e.g., `Handle_MaxMenteesBelowActive_ShouldFail`)
- Test class name matches the class under test: `UpdateSettingsHandlerTests`

### Unit Tests (xUnit + FluentAssertions)
- One assertion concept per test (multiple `Should()` calls for the same concept are fine)
- Use `NSubstitute` for mocking interfaces
- Use `Bogus` for realistic test data generation
- Arrange-Act-Assert pattern with clear section comments

### Property-Based Tests (FsCheck)
- Minimum 100 iterations per property
- Tag with `[Trait("Category", "Property")]`
- Property name format: `Property{N}_{Description}` (e.g., `Property6_MatchingScoreBoundsAndDeterminism`)
- Use custom Arbitraries for domain-specific generators

### Frontend Tests (Vitest + React Testing Library)
- Test user behavior, not implementation details
- Use `screen.getByRole()` over `getByTestId()` where possible
- Include axe-core checks (`expect(await axe(container)).toHaveNoViolations()`)
- Mock API calls with MSW (Mock Service Worker) or TanStack Query's test utilities

### Coverage Thresholds
- Domain/pure logic: ≥95% line coverage
- Application handlers: ≥80% line coverage
- Infrastructure: best-effort (external dependencies)
- Frontend components: ≥70% (focus on interaction tests, not snapshot)

## Terraform Conventions

- Module structure: one `.tf` file per logical resource group
- Naming: `${var.environment}-guidedmentor-{resource}`
- Required tags: Environment, Service, BoundedContext
- Conditional resources: `count = var.enable_{feature} ? 1 : 0`
- Data stores: `lifecycle { prevent_destroy = true }`
- DynamoDB: on-demand billing, PITR enabled (future AWS migration)
- S3: block public access, versioning, encryption (future AWS migration)

## Frontend Design Tokens

- Colors: use Tailwind theme variables (`text-violet`, `bg-mint/20`) — never hardcode hex
- Headings font: Outfit (via inline style `fontFamily`)
- Body font: Inter (CSS default)
- Cards: `glass-card` class (backdrop-blur + subtle border)
- Scores: `<ScoreRing>` component with mint/violet/amber color by value
- Buttons: `btn-violet`, `btn-mint`, `btn-ghost` utility classes
- Loading states: use Skeleton components matching page layout
