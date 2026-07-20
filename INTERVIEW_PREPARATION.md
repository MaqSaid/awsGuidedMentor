# Interview Preparation — .NET Full Stack + Azure Core Services

> Based on GuidedMentor project (AWS) mapped to Azure equivalents.  
> JD: .NET Core, React.js, Next.js, Azure Core Services, OpenID Connect, OAuth 2.0, Azure DevOps, Agile

---

## Part 1: Azure ↔ AWS Service Mapping Table

| JD Requirement | Azure Service | AWS Equivalent (Your Project) | Your GuidedMentor Example |
|---|---|---|---|
| **Scalable Deployments** | Azure App Services | AWS Lambda (Native AOT) | `deploy-backend.yml` deploys .NET 10 Lambda per bounded context with traffic shifting (10% → 100%) |
| **Secret Management** | Azure Key Vault | AWS Secrets Manager / Parameter Store | JWT signing keys, Gmail SMTP credentials, Google OAuth secrets stored in SSM; Terraform references `var.google_client_secret` |
| **Async Messaging** | Azure Service Bus | Amazon EventBridge + SQS (DLQ) | `events/main.tf`: custom event bus routes `SessionAccepted` → Content Lambda, `CompletionMarked` → Engagement Lambda; SQS DLQs for failed messages |
| **Identity Provider** | Azure AD B2C | AWS Cognito User Pool | `identity/main.tf`: Cognito with magic link (ALLOW_CUSTOM_AUTH), Google IdP, OIDC scopes `openid email profile` |
| **OpenID Connect** | Azure AD B2C OIDC | Cognito Hosted UI + OIDC | OAuth 2.0 Authorization Code flow (`allowed_oauth_flows = ["code"]`), JWT access/id/refresh tokens |
| **OAuth 2.0** | Azure AD OAuth | Cognito OAuth 2.0 | API Gateway Cognito Authorizer validates JWTs; 15-min access tokens, 7-day refresh tokens |
| **CI/CD Pipelines** | Azure DevOps Pipelines | GitHub Actions | `ci-dotnet.yml`, `deploy-backend.yml`, `deploy-infrastructure.yml` — build, test, coverage gates, Terraform plan/apply |
| **API Management** | Azure API Management | AWS API Gateway | `networking/main.tf`: REST API with Cognito authorizer, rate limiting (100 req/s), WAF, CloudWatch logging |
| **CDN / Static Hosting** | Azure CDN + Blob Storage | CloudFront + S3 | `networking/main.tf`: S3 bucket for SPA with OAI, CloudFront distribution, security headers (CSP, HSTS) |
| **Database** | Azure SQL / Cosmos DB | PostgreSQL (EF Core) / DynamoDB | `GuidedMentorDbContext` with 9 DbSets, JSONB columns, snake_case mapping; DynamoDB for users table in production |
| **Background Jobs** | Azure Functions (Timer) | EventBridge Scheduler / Hangfire | `CleanupExpiredTokensJob` (every 5 min), `OpportunityExpiryJob` (daily); EventBridge `rate(5 minutes)` for lock cleanup |
| **Real-time** | Azure SignalR Service | Self-hosted SignalR (WebSocket) | `NotificationHub` at `/hubs/notifications`, user-grouped by JWT claim |
| **Monitoring** | Azure Monitor + App Insights | CloudWatch + X-Ray | `PerformanceBehavior` logs >500ms requests; API Gateway access logs to CloudWatch |
| **WAF** | Azure Front Door WAF | AWS WAF v2 | `security/main.tf`: WAF Web ACL associated with API Gateway and CloudFront |
| **File Storage** | Azure Blob Storage | S3 | Resume storage bucket with versioning, encryption, lifecycle policies (Glacier after 30 days) |

---

## Part 2: Interview Questions & Answers with Project Examples

---

### Q1: Explain your experience with .NET Core and building scalable backend systems.

**Answer:**

In GuidedMentor, I built a .NET 10 backend using Clean Architecture with four bounded contexts — Identity, Mentoring, Content, and Engagement. Each context is an independent deployable unit (Lambda function) following Domain-Driven Design.

**Project Example — MediatR CQRS Pipeline:**
```csharp
// ServiceCollectionExtensions.cs — Pipeline behavior registration order
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(assembliesToScan);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));    // 1. FluentValidation
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));       // 2. Structured logging
    cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));  // 3. Audit trail
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));   // 4. Slow-query detection
});
```

**Azure Mapping:** In Azure, I'd deploy these as separate Azure App Services (or Azure Functions for event-driven workloads) behind Azure API Management, each with its own Azure SQL Database — same bounded context isolation, different hosting model.

---

### Q2: How do you design and develop RESTful APIs?

**Answer:**

I use ASP.NET Minimal APIs with versioned endpoints, MediatR for CQRS, and a consistent error response envelope.

**Project Example — Auth Endpoints:**
```csharp
// AuthEndpoints.cs — Versioned, grouped, documented
var auth = app.MapGroup("/v1/auth").WithTags("Authentication");

auth.MapPost("/magic-link", async (MagicLinkRequest request, IMediator mediator, CancellationToken ct) =>
{
    var command = new RequestMagicLinkCommand(request.Email);
    await mediator.Send(command, ct);
    // Always return 200 to prevent email enumeration
    return Results.Ok(new { message = "If this email is registered, you'll receive a sign-in link." });
})
.WithName("RequestMagicLink")
.Produces(StatusCodes.Status200OK)
.AllowAnonymous();
```

**Design Principles Applied:**
- Plural nouns: `/v1/sessions`, `/v1/mentors`
- Actions as sub-paths: `/v1/sessions/{id}/complete`
- Consistent error shape: `{ statusCode, error, message, correlationId }`
- Security: always 200 on auth endpoints (prevents enumeration)

**Azure Mapping:** Azure API Management provides the same API gateway features (rate limiting, JWT validation, CORS) that our AWS API Gateway + Cognito Authorizer provides.

---

### Q3: How have you integrated Entity Framework with relational databases?

**Answer:**

GuidedMentor uses EF Core with PostgreSQL in the local/free tier and DynamoDB in production AWS. The EF Core layer acts as a persistence adapter.

**Project Example — DbContext Configuration:**
```csharp
// GuidedMentorDbContext.cs
public sealed class GuidedMentorDbContext : DbContext
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<MentorEntity> Mentors => Set<MentorEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<AuthTokenEntity> AuthTokens => Set<AuthTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MentorEntity>(e =>
        {
            e.ToTable("mentors");
            e.Property(x => x.Availability).HasColumnName("availability").HasColumnType("jsonb");
            e.Property(x => x.ExpertiseAreas).HasColumnName("expertise_areas"); // TEXT[]
        });
    }
}
```

**Key Patterns:**
- Persistence entities are separate from Domain entities (mapping in repositories)
- JSONB columns for complex nested objects (availability schedules)
- `ExecuteUpdateAsync` / `ExecuteDeleteAsync` for bulk operations without loading entities
- Connection string from `ConnectionStrings:DefaultConnection`

**Azure Mapping:** Azure SQL Database + EF Core works identically. Azure Key Vault stores the connection string instead of environment variables. For NoSQL needs, Cosmos DB replaces DynamoDB.

---

### Q4: Describe your experience with Azure App Services and Azure Key Vault (secure deployments).

**Answer (mapped from AWS):**

In GuidedMentor, I deploy .NET 10 Native AOT Lambdas behind API Gateway — the Azure equivalent is App Service or Azure Functions. For secrets, I use AWS SSM Parameter Store and Secrets Manager — the Azure equivalent is Key Vault.

**Project Example — Deployment Pipeline:**
```yaml
# deploy-backend.yml — Traffic shifting deployment
- name: Deploy with traffic shifting
  run: |
    # 10% traffic to new version, monitor 2 minutes, then 100%
    aws lambda update-alias \
      --function-name "${FUNCTION_NAME}" \
      --name "live" \
      --function-version "${CURRENT_VERSION}" \
      --routing-config "AdditionalVersionWeights={${VERSION}=0.1}"
```

**How I'd do this in Azure:**
- **App Service Deployment Slots**: Blue/green deployment using staging slots with traffic routing (same concept as Lambda aliases with weighted routing)
- **Azure Key Vault**: Reference secrets as `@Microsoft.KeyVault(SecretUri=...)` in App Service configuration
- **Managed Identity**: App Service uses system-assigned managed identity to access Key Vault (no credentials in code — same as IAM roles for Lambda)

---

### Q5: How do you implement asynchronous messaging for decoupled microservices?

**Answer:**

GuidedMentor uses EventBridge as a custom event bus for cross-context communication, with SQS dead-letter queues for failure handling. The Azure equivalent is Azure Service Bus.

**Project Example — Event-Driven Architecture:**
```hcl
# events/main.tf — Cross-context event routing
resource "aws_cloudwatch_event_rule" "session_accepted" {
  name           = "${local.name_prefix}-session-accepted"
  event_bus_name = aws_cloudwatch_event_bus.main.name

  event_pattern = jsonencode({
    source      = ["guidedmentor.mentoring"]
    detail-type = ["SessionAccepted"]
  })
}
```

**Application Code — Publishing Events:**
```csharp
// MarkCompleteHandler.cs — Publish domain events after state change
await _eventBridgePublisher.ScheduleCompletionReminderAsync(
    session.Id.Value, session.MentorId.Value, reminderDate, cancellationToken);

await _eventBridgePublisher.ScheduleEscalationAsync(
    session.Id.Value, escalationDate, cancellationToken);
```

**Azure Service Bus Mapping:**
| AWS (My Project) | Azure Service Bus Equivalent |
|---|---|
| EventBridge custom bus | Service Bus Topic |
| Event rules (pattern matching) | Topic Subscriptions with filters |
| SQS Dead-Letter Queue | Service Bus built-in DLQ |
| EventBridge Scheduler | Azure Functions Timer Trigger |
| `source = ["guidedmentor.mentoring"]` | Topic filter on `Source` property |

---

### Q6: Explain your authentication/authorization implementation using OpenID Connect and OAuth 2.0.

**Answer:**

GuidedMentor implements passwordless authentication using magic links + Google OAuth, issued via AWS Cognito (OIDC-compliant identity provider). The Azure equivalent is Azure AD B2C.

**Project Example — Cognito OAuth Configuration:**
```hcl
# identity/main.tf
resource "aws_cognito_user_pool_client" "web" {
  allowed_oauth_flows  = ["code"]           # Authorization Code flow (PKCE for SPA)
  allowed_oauth_scopes = ["openid", "email", "profile"]
  
  explicit_auth_flows = [
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_CUSTOM_AUTH"   # For magic link (custom challenge)
  ]

  access_token_validity  = 15   # 15 minutes
  refresh_token_validity = 7    # 7 days
  prevent_user_existence_errors = "ENABLED"  # Security
}
```

**Application Code — JWT Token Handling:**
```csharp
// AuthEndpoints.cs — Token verification
auth.MapPost("/verify-magic-link", async (VerifyMagicLinkRequest request, IMediator mediator) =>
{
    var command = new VerifyMagicLinkCommand(request.Email, request.Token);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)  // Returns { accessToken, refreshToken, idToken }
        : Results.BadRequest(new { error = result.Error });
});
```

**Azure AD B2C Mapping:**
| Concept | AWS Cognito (My Project) | Azure AD B2C |
|---|---|---|
| Identity Provider | Cognito User Pool | Azure AD B2C Tenant |
| Custom Auth Flow | Custom Challenge Lambda | Custom Policies (IEF) |
| Google Federation | Cognito Identity Provider | B2C Social Identity Provider |
| JWT Validation | API Gateway Cognito Authorizer | Azure APIM JWT validation policy |
| Token Refresh | ALLOW_REFRESH_TOKEN_AUTH | B2C refresh token flow |
| OIDC Scopes | openid, email, profile | Same OIDC standard scopes |

---

### Q7: Describe your experience with React.js and component-based architecture.

**Answer:**

GuidedMentor's frontend is a React 19 SPA with TypeScript, TanStack Query for server state, React Router v7 for routing, and Module Federation for micro-frontend architecture.

**Project Example — App.tsx (Lazy Loading + Accessibility):**
```tsx
// Lazy-loaded routes (code-split per page)
const MenteeDashboard = lazy(() => import('./pages/MenteeDashboard'));
const SessionPlan = lazy(() => import('./pages/SessionPlan'));

function App() {
  return (
    <>
      {/* Skip-nav link for accessibility (WCAG 2.1 AA) */}
      <a href="#main-content" className="sr-only focus:not-sr-only ...">
        Skip to main content
      </a>

      <main id="main-content">
        <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/dashboard" element={<DashboardRouter />} />
            <Route path="/sessions/:id/plan" element={<SessionPlan />} />
          </Routes>
        </Suspense>
      </main>
    </>
  );
}
```

**Custom Hooks Pattern:**
```tsx
// useOptimistic.ts — Optimistic UI updates with rollback
export function useOptimisticToggle(serverState: boolean, updateFn: (v: boolean) => Promise<void>) {
  const [optimistic, setOptimistic] = useState(serverState);
  const toggle = useCallback(async () => {
    setOptimistic(!optimistic);       // Optimistic update
    try { await updateFn(!optimistic); }
    catch { setOptimistic(serverState); }  // Rollback on failure
  }, [optimistic, serverState, updateFn]);
  return [optimistic, toggle] as const;
}
```

---

### Q8: How do you handle CI/CD pipelines? (Azure DevOps context)

**Answer:**

GuidedMentor uses GitHub Actions for CI/CD — the concepts map directly to Azure DevOps Pipelines.

**Project Example — CI Pipeline with Coverage Gates:**
```yaml
# ci-dotnet.yml
services:
  postgres:
    image: postgres:16
    env:
      POSTGRES_DB: guidedmentor_test

steps:
  - name: Run tests with coverage
    run: dotnet test --collect:"XPlat Code Coverage"
    
  - name: Check coverage thresholds
    run: |
      # Domain/pure logic: ≥95% line coverage
      # Application handlers: ≥80% line coverage
      if [ "$DOMAIN_INT" -lt 95 ]; then
        echo "::error::Domain coverage is ${DOMAIN_COV}% (minimum: 95%)"
      fi
```

**Azure DevOps Mapping:**
| GitHub Actions (My Project) | Azure DevOps Equivalent |
|---|---|
| `.github/workflows/*.yml` | `azure-pipelines.yml` |
| `on: pull_request` | `trigger: branches` / `pr:` |
| `services: postgres` | Service Containers in pipeline |
| `actions/checkout@v4` | `- checkout: self` |
| `hashicorp/setup-terraform@v3` | Terraform extension task |
| GitHub Environments + Secrets | Variable Groups + Key Vault integration |
| `workflow_dispatch` | Manual triggers / Approvals |
| Matrix strategy | Strategy matrix in YAML |

---

### Q9: How do you handle background processing and scheduled tasks?

**Answer:**

GuidedMentor uses Hangfire (PostgreSQL-backed) for local development and AWS EventBridge Scheduler for production. The Azure equivalent is Azure Functions Timer Triggers or Azure Durable Functions.

**Project Example — Hangfire Background Jobs:**
```csharp
// CleanupExpiredTokensJob.cs — Recurring every 5 minutes
public sealed class CleanupExpiredTokensJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<CleanupExpiredTokensJob> _logger;

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting expired token cleanup job");
        var deleted = await _db.AuthTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.Used)
            .ExecuteDeleteAsync();
        _logger.LogInformation("Cleaned up {Count} expired/used auth tokens", deleted);
    }
}

// Registration in Program.cs
RecurringJob.AddOrUpdate<CleanupExpiredTokensJob>(
    "cleanup-tokens", j => j.ExecuteAsync(), "*/5 * * * *");
RecurringJob.AddOrUpdate<OpportunityExpiryJob>(
    "expire-opportunities", j => j.ExecuteAsync(), "0 0 * * *");
```

**Azure Mapping:**
- **Azure Functions Timer Trigger** replaces EventBridge Scheduler (cron expressions are identical)
- **Azure Durable Functions** for orchestration workflows (completion reminders, escalation chains)
- **Azure Queue Storage / Service Bus** for fire-and-forget (replaces SQS)

---

### Q10: Explain your approach to Domain-Driven Design and Clean Architecture.

**Answer:**

GuidedMentor has four bounded contexts, each following strict layering:

```
src/
├── Identity/           ← Bounded Context
│   ├── Domain/         ← Zero dependencies, pure business logic
│   ├── Application/    ← Commands, Queries, Handlers, Interfaces
│   ├── Infrastructure/ ← EF Core repos, external services
│   └── Api/            ← Minimal API endpoints (thin HTTP layer)
├── Mentoring/
├── Content/
└── Engagement/
```

**Project Example — Aggregate Root with Domain Events:**
```csharp
// User.cs (Identity Domain)
public sealed class User : AggregateRoot<UserId>
{
    public Result ToggleRole()
    {
        if (ActiveRole is null)
            return Result.Failure("Cannot toggle role. No active role has been set.");

        var previousRole = ActiveRole.Value;
        ActiveRole = previousRole == Role.Mentor ? Role.Mentee : Role.Mentor;
        
        RaiseDomainEvent(new RoleToggledEvent(Id, previousRole, ActiveRole.Value, DateTime.UtcNow));
        return Result.Success();
    }
}
```

**Key DDD Patterns:**
- Aggregate roots enforce invariants (User can't toggle role without initial selection)
- Domain events for cross-context side effects (RoleToggledEvent → Mentoring context updates)
- Result pattern instead of exceptions for business logic failures
- Repository per aggregate root, interface in Application, implementation in Infrastructure

---

### Q11: How do you ensure real-time communication in your application?

**Answer:**

GuidedMentor uses SignalR for real-time notifications. In Azure, this maps to Azure SignalR Service (managed WebSocket infrastructure).

**Project Example — SignalR Hub:**
```csharp
// NotificationHub.cs
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;  // From JWT claim
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }
}

// Program.cs — Registration
app.MapHub<NotificationHub>("/hubs/notifications");
```

**Azure SignalR Service Benefits:**
- Managed infrastructure (no WebSocket server scaling concerns)
- Azure AD authentication integration
- Same `IHubContext<NotificationHub>` programming model
- Serverless mode compatible with Azure Functions

---

### Q12: How do you validate inputs in your API?

**Answer:**

GuidedMentor uses FluentValidation with auto-discovery, executed in a MediatR pipeline behavior before the handler runs.

**Project Example — Validation Pipeline:**
```csharp
// ValidationBehavior.cs — Runs BEFORE every handler
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// SetRoleCommandValidator.cs
public sealed class SetRoleCommandValidator : AbstractValidator<SetRoleCommand>
{
    public SetRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Role must be Mentor or Mentee.");
    }
}
```

---

### Q13: What is your experience with Next.js (SSR)?

**Answer:**

While GuidedMentor's frontend is a Vite-based SPA (React 19 + React Router), I understand Next.js patterns and can discuss the architectural tradeoffs:

**What GuidedMentor Uses (SPA):**
- React 19 with lazy loading (`React.lazy()` + `Suspense`)
- Module Federation for micro-frontend architecture
- MSW (Mock Service Worker) for API mocking during development
- TanStack Query for server-state caching (similar to SWR in Next.js)

**How I'd migrate to Next.js:**
- Pages like `/browse` (mentor directory) → SSR for SEO and initial load performance
- Dashboard pages → Client-side rendering (authenticated, no SEO needed)
- Session plan generation → Server Actions for streaming AI responses
- `usePrefetch` hook → Next.js `<Link prefetch>` (built-in)

**Azure Hosting for Next.js:**
- Azure Static Web Apps (supports Next.js SSR natively)
- Azure App Service (custom Node.js container)
- Vercel (GuidedMentor already has `.vercel/` configuration)

---

### Q14: How do you handle audit logging and observability?

**Answer:**

GuidedMentor has a cross-cutting audit logging pipeline that records who did what, when, and whether it succeeded.

**Project Example — Audit Logging Behavior:**
```csharp
// AuditLoggingBehavior.cs
public sealed class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IAuditableCommand auditableCommand)
            return await next();

        // Records: who (userId), when (UTC), what (command type), which resource, correlationId
        var record = new AuditLogRecord {
            UserId = command.UserId.ToString(),
            Action = typeof(TRequest).Name,
            Resource = command.AuditResourceId,
            CorrelationId = CorrelationContext.CurrentCorrelationId
        };

        // For admin operations, additionally log adminId, target, reason
        if (command is IAdminCommand adminCommand)
            record = record with { AdminId = adminCommand.AdminId.ToString() };

        await _auditLogWriter.WriteAsync(record, ct);
    }
}
```

**Azure Mapping:**
- CloudWatch → **Azure Monitor** (metrics + logs)
- CloudWatch Log Groups → **Log Analytics Workspace**
- X-Ray → **Application Insights** (distributed tracing)
- `PerformanceBehavior` (>500ms warning) → App Insights **dependency tracking**

---

### Q15: How do you work in an Agile environment?

**Answer:**

GuidedMentor demonstrates Agile practices through:

1. **Bounded context decomposition** — each context (Identity, Mentoring, Content, Engagement) maps to a team or sprint focus area
2. **Feature flags** — `AddFeatureFlags(builder.Configuration)` enables trunk-based development with dark launches
3. **CI gates** — every PR runs build + test + coverage checks automatically
4. **Incremental delivery** — traffic shifting deploys (10% → 100%) allow safe, gradual rollouts
5. **Infrastructure as Code** — Terraform plan on PR, apply on merge (reviewed like application code)
6. **Observability-first** — `PerformanceBehavior` and audit logging enable data-driven sprint retrospectives

**Azure DevOps Specific:**
- Azure Boards for sprint planning and backlog management
- Pull Request policies with required reviewers and build validation
- Release pipelines with approval gates (staging → production)
- Azure Test Plans for manual test scenarios

---

## Part 3: Quick-Fire Technical Differentiators

| Topic | What to Say in Interview |
|---|---|
| **Architecture** | "Clean Architecture with DDD — 4 bounded contexts, MediatR CQRS, domain events for cross-context communication" |
| **Error Handling** | "Result pattern for business logic, exceptions only for infrastructure failures, Polly v8 for resilience" |
| **Security** | "Passwordless auth (magic link), OAuth 2.0 Authorization Code + PKCE, prevent user enumeration, WAF, CSP headers" |
| **Testing** | "95% coverage on domain logic, 80% on handlers, property-based tests with FsCheck, E2E with Playwright" |
| **Performance** | "Native AOT compilation, lazy-loaded React routes, EventBridge for async processing, CloudFront CDN" |
| **Database** | "EF Core with PostgreSQL, JSONB for complex objects, repository pattern, separate persistence vs domain models" |
| **DevOps** | "GitHub Actions CI/CD (Azure DevOps equivalent), Terraform IaC, traffic-shifting deploys, automated coverage gates" |

---

## Part 4: Questions to Ask the Interviewer

1. "What Azure services does the team currently use for async messaging — Service Bus Topics or Event Grid?"
2. "Are you using Azure AD B2C or a custom identity solution for authentication?"
3. "How do you handle deployment strategies — App Service slots, AKS blue/green, or Azure Functions?"
4. "What's the team's approach to Infrastructure as Code — ARM templates, Bicep, or Terraform?"
5. "How is the codebase structured — monolith, modular monolith, or microservices?"


---

## Part 5: Key React & .NET Features Used in GuidedMentor (Interview Talking Points)

---

### React Key Features

**1. React 19 with Functional Components Only**
- Latest version, no class components — everything uses hooks
- "We chose React 19 for the latest concurrent rendering capabilities and hook-based architecture"

**2. Code Splitting with `React.lazy()` + `Suspense`**
- Every page route is lazy-loaded — only downloads JS when the user navigates there
- Reduces initial bundle size significantly for a multi-page app
- "Like a buffet where dishes are cooked only when someone walks up to that station"

**3. Module Federation (Micro-Frontend Architecture)**
- 4 separate federated remotes: identity, mentoring, content, engagement
- Host-shell orchestrates them — each team can deploy independently
- Uses `@originjs/vite-plugin-federation`
- "Like a shopping mall — each store (remote) operates independently, but they share the same building (host-shell)"

**4. TanStack React Query (Server State Management)**
- All API data fetching uses React Query — no `useEffect` for data
- Automatic caching, background refetching, stale-while-revalidate
- Separates server state from client state cleanly
- "We don't use Redux or global state for API data — React Query handles caching, deduplication, and revalidation out of the box"

**5. React Router DOM 7 (Client-Side Routing)**
- Declarative routing with nested routes
- Parameterized routes (`/sessions/:id/plan`)
- Role-based routing logic (mentor vs mentee dashboard)

**6. Context API for Auth State**
- `AuthContext` provides user/role info across the component tree
- Lightweight — no external state library needed for auth

**7. TanStack Virtual (Virtualized Lists)**
- Renders only visible items in long lists (mentor browse, notifications)
- Handles thousands of items without DOM bloat
- "Only the items in the viewport are rendered — scroll and items swap in/out"

**8. MSW (Mock Service Worker)**
- API mocking at the network level during development
- Team can build frontend before backend is ready
- Same mocks used in tests — no divergence

**9. Vite 6 (Build Tooling)**
- Native ES modules for instant HMR in development
- Rollup-based production builds with tree-shaking
- TailwindCSS 4 Vite plugin for zero-config CSS

**10. Accessibility (WCAG 2.1 AA)**
- Skip-nav link as first focusable element
- Semantic HTML (`<nav>`, `<main>`, `<article>`)
- `aria-live` for dynamic content, focus trapping in modals
- axe-core in E2E tests (automated accessibility regression)

**11. TypeScript 5.7 (Strict Mode)**
- `noUncheckedIndexedAccess`, strict null checks
- Catches entire categories of bugs at compile time

**12. PWA Support**
- Service worker for offline caching
- Web app manifest for install-to-homescreen

---

### .NET Key Features

**1. .NET 10 (LTS) + C# Latest**
- Long-term support until 2028
- Primary constructors, records, file-scoped namespaces, nullable reference types enforced as errors

**2. ASP.NET Core Minimal APIs**
- No controllers — endpoints mapped directly with `app.MapGet/Post()`
- Less ceremony, better AOT compatibility, faster startup
- "Like Express.js routing but with full type safety"

**3. Clean Architecture (4 Layers)**
- Domain → Application → Infrastructure → API
- Dependency rule: inner layers never reference outer
- Each bounded context (Identity, Mentoring, Content, Engagement) is independent

**4. MediatR (CQRS Pattern)**
- Commands separate writes from reads (queries)
- Pipeline behaviors: Validation → Logging → Audit → Performance → Handler
- Decouples HTTP layer from business logic entirely
- "The API layer just wraps the request and sends it — doesn't know how it's handled"

**5. FluentValidation**
- Declarative rules for every command/query
- Auto-discovered per assembly — no manual registration
- Runs in MediatR pipeline before the handler executes

**6. Result Pattern (No-Exception Business Logic)**
- Handlers return `Result<T>` instead of throwing
- Exceptions reserved for infrastructure failures only
- API maps Result to appropriate HTTP status codes

**7. Entity Framework Core + PostgreSQL (Npgsql)**
- Code-first with migrations
- JSONB columns for nested data, TEXT[] for arrays
- Separate persistence models from domain entities (no leaky abstraction)

**8. Repository Pattern**
- Interface in Application layer, implementation in Infrastructure
- One repository per aggregate root
- Returns domain entities, never DTOs

**9. SignalR (Real-Time Notifications)**
- WebSocket hub at `/hubs/notifications`
- Pushes session updates, match notifications instantly
- Fallback to long-polling if WebSocket fails

**10. Hangfire (Background Jobs)**
- PostgreSQL-backed job storage
- Recurring jobs: token cleanup (every 5 min), notification digests, availability reminders
- Fire-and-forget for async email sends

**11. Polly 8 (Resilience)**
- Retry policies for HTTP calls (Bedrock API, external services)
- Circuit breaker, timeout, bulkhead isolation
- No manual retry loops — declarative pipeline

**12. Passwordless Auth (Magic Link + JWT)**
- Self-issued JWT tokens, 10-minute TTL, single-use
- No passwords stored anywhere
- Rate-limited (3 per email per 15 min), constant-time response to prevent enumeration

**13. OpenTelemetry (Observability)**
- Distributed tracing across all API calls
- OTLP exporter for traces + metrics
- ASP.NET Core + HTTP client instrumentation auto-wired

**14. Serilog (Structured Logging)**
- Compact JSON format for machine parsing
- Correlation IDs across requests
- Context-enriched logs (user ID, session ID, bounded context)

**15. Native AOT Readiness**
- `System.Text.Json` source generation (no reflection)
- No dynamic proxies
- Ready to compile AOT for sub-100ms Lambda cold starts

**16. OpenAPI + Scalar**
- Auto-generated API documentation from endpoints
- Scalar UI (modern replacement for Swagger UI)
- Spec used for client SDK generation

**17. Domain-Driven Design**
- Aggregate roots enforce invariants
- Value objects for type safety (UserId, SessionId)
- Domain events for cross-context side effects via MediatR notifications

**18. Feature Flags (AWS AppConfig)**
- Runtime feature toggling without redeployment
- Gradual rollouts, A/B testing capability

---

### How to Frame in an Interview

When asked "What tech stack did you use?", structure it as:

> "The frontend is React 19 with TypeScript, using a micro-frontend architecture via Module Federation. We use TanStack Query for server state, React Router for navigation, and everything is code-split with lazy loading. The backend is .NET 10 with Clean Architecture — MediatR for CQRS, FluentValidation in the pipeline, EF Core with PostgreSQL, SignalR for real-time, and Hangfire for background jobs. We chose the Result pattern over exceptions for predictable error handling, and the whole system is observable with OpenTelemetry and Serilog."

When asked "Why did you choose X?", always answer with:
1. The **problem** it solves
2. What the **alternative** was
3. The **tradeoff** you accepted
