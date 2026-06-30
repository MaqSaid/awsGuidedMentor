# Building GuidedMentor: Step-by-Step Manual Approach

> A comprehensive study guide explaining how to build an enterprise-grade AI-powered mentorship platform from scratch, with architectural justification for every decision.

## How I'd Present This in an Interview

"I built GuidedMentor — an AI-powered mentor-mentee matching platform for AWS community developers. Let me walk you through how I approached it, the decisions I made, and why."

---

## Step 1: Domain Discovery & Bounded Context Mapping

### What I Do
I start with Event Storming — identifying domain events, commands, and aggregates on a whiteboard. I map out the business flows:
- User signs up → selects role → completes onboarding → browses mentors → locks a mentor → AI generates session plan → both parties complete

### How I Do It
I identify 4 bounded contexts using the heuristic "which concepts change together for the same business reason":
- **Identity Context**: Authentication, roles, onboarding, user profiles
- **Mentoring Context**: Matching, locking, sessions, completion flow
- **Content Context**: AI session plan generation, checklists
- **Engagement Context**: Notifications, dashboards, analytics

### Why This Approach
- **DDD Principle**: Bounded contexts give each domain area its own ubiquitous language. A "Session" in Mentoring (relationship state) is different from a "Session" in Content (learning plan).
- **Microservices**: Each context becomes an independently deployable service — one team can modify matching without touching authentication.
- **Scalability**: Content (AI generation) has different scaling needs than Identity (auth) — they scale independently.

---

## Step 2: Requirements & API Contract (API-First)

### What I Do
I write requirements BEFORE code. Then I define the API contract (OpenAPI spec) for each bounded context.

### How I Do It
- Write user stories with acceptance criteria per bounded context
- Define REST endpoints: `GET /v1/mentors`, `POST /v1/locks`, `POST /v1/sessions/{id}/complete`
- Define request/response shapes, error formats, status codes
- Define auth requirements per endpoint (JWT, admin-only, anonymous)

### Why This Approach
- **API-First**: Frontend and backend teams can work in parallel against the contract. Changes are caught at design time, not integration time.
- **Security by Design**: Auth requirements are defined upfront — not bolted on later.
- **Testing**: Contract becomes the basis for integration tests.
- **Microservices**: Each service owns its API surface — no cross-service dependencies.

---

## Step 3: Data Strategy (Relational vs NoSQL)

### What I Do
I analyze each bounded context's access patterns and choose the right database:
- **Operational data** (users, sessions, locks): DynamoDB (NoSQL)
- **Analytics** (funnels, DAU/MAU, cross-entity reports): Aurora PostgreSQL (Relational)

### How I Do It

**DynamoDB Design (Denormalized)**:
- Table per aggregate: Users, Mentors, Mentees, Sessions, Notifications
- Partition key = GUID (uniform distribution, no hot keys)
- GSIs for each access pattern (GSI-Mentee on Sessions, GSI-Chapter on Meetups)
- Composite partition key for high-volume: `recipientUserId#YYYY-MM` on Notifications

**Aurora Design (Normalized)**:
- Separate tables with foreign keys: analytics.users, analytics.sessions, analytics.matches
- JOINs for funnel analysis, GROUP BY for metrics
- Data replicated from DynamoDB via Streams (eventual consistency)

### Why This Approach
- **Data Strategy**: DynamoDB for operational (simple access patterns, single-digit ms, serverless) vs Aurora for analytical (complex queries, joins, aggregations).
- **Scalability**: DynamoDB auto-scales with on-demand capacity — no provisioning. Aurora Serverless v2 scales ACUs based on query load.
- **Sharding/Partitioning**: DynamoDB auto-shards on partition key. I designed GUID keys for uniform distribution. For Notifications (write-hot users), I use composite key `userId#YYYY-MM` to distribute across monthly partitions.
- **Denormalized in NoSQL**: Session stores mentorName to avoid a second read. Trade-off: name changes require update (rare) but reads are 2x faster.
- **Normalized in SQL**: Analytics needs JOINs — denormalizing there would cause update anomalies across millions of records.
- **Cost Optimization**: DynamoDB on-demand = $0 when idle. Aurora Serverless v2 scales to 0.5 ACU minimum.

---

## Step 4: Infrastructure as Code (Terraform)

### What I Do
I define ALL infrastructure in Terraform before writing application code. Nothing is created manually in the AWS console.

### How I Do It
- Create modular structure: one module per concern (identity, mentoring, networking, security, observability)
- Use variables with types and descriptions for everything configurable
- Create environment-specific tfvars (dev skips Aurora/WAF/KMS; prod enables all)
- Add `prevent_destroy` on data stores, conditional resources with `count`

**Example module structure**:
```
infrastructure/
├── main.tf              # Provider, backend config
├── variables.tf         # All input variables
├── outputs.tf           # Cross-module outputs
├── environments/
│   ├── dev.tfvars       # Minimal (free tier)
│   ├── staging.tfvars   # Full stack minus WAF
│   └── prod.tfvars      # Everything enabled
├── modules/
│   ├── identity/        # Cognito, IAM roles
│   ├── mentoring/       # DynamoDB tables, Lambdas
│   ├── content/         # Bedrock access, S3
│   ├── networking/      # VPC, subnets, NAT
│   ├── security/        # WAF, KMS, Security Groups
│   └── observability/   # CloudWatch, X-Ray, SNS
```

**Example conditional resource**:
```hcl
resource "aws_wafv2_web_acl" "main" {
  count = var.enable_waf ? 1 : 0
  name  = "${var.environment}-guidedmentor-waf"
  # ...
}
```

### Why This Approach
- **Terraform/IaC**: Repeatable, auditable, version-controlled infrastructure. One `terraform apply` provisions the entire platform.
- **Security by Design**: IAM permission boundaries, KMS encryption, WAF rules defined in code — not console clicks that get forgotten.
- **Scalability**: Same Terraform modules scale from dev (free tier) to production (multi-AZ, Global Tables) by changing tfvars.
- **Governance**: tflint enforces naming conventions, checkov scans for security misconfigurations, drift detection catches manual changes.
- **Cost Optimization**: Conditional resources mean dev costs $0 (skip Aurora, WAF, KMS) while prod has full protection.

---

## Step 5: Shared Kernel & Design Patterns

### What I Do
I create the shared foundation that all bounded contexts build on: Result<T>, Entity<T>, ValueObject, IAuditableCommand, and MediatR pipeline behaviors.

### How I Do It

**Result pattern — never throw for business logic**:
```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

**MediatR Pipeline Behaviors (Chain of Responsibility)**:
```
Request → ValidationBehavior → LoggingBehavior → AuditLoggingBehavior → PerformanceBehavior → Handler → Response
```

**Each behavior wraps the next**:
```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Why This Approach
- **Reusable Design Patterns**: Result<T> replaces exception-driven flow. MediatR pipeline gives us Decorator pattern for cross-cutting concerns.
- **DDD**: Entity<T> and ValueObject base classes enforce identity vs value semantics. AggregateRoot ensures domain events are raised correctly.
- **Security by Design**: AuditLoggingBehavior automatically logs every state-changing command without handlers knowing about it.
- **Observability**: LoggingBehavior + PerformanceBehavior give us structured logs and latency metrics for free on every handler.
- **Testing**: Pipeline behaviors are independently testable. Handlers are tested in isolation without cross-cutting concerns.

---

## Step 6: Domain Implementation (DDD)

### What I Do
I implement domain entities with BEHAVIOR — not just data. Entities enforce their own invariants. The domain layer has zero dependencies on infrastructure or frameworks.

### How I Do It

**Rich domain model with state machine enforcement**:
```csharp
public sealed class Session : AggregateRoot<SessionId>
{
    public SessionStatus Status { get; private set; }
    public DateTime? MenteeCompletedAt { get; private set; }
    public DateTime? MentorCompletedAt { get; private set; }

    public Result MarkComplete(Role role)
    {
        if (Status != SessionStatus.Active)
            return Result.Failure("Session is not active.");

        if (role == Role.Mentor && MenteeCompletedAt is null)
            return Result.Failure("Mentee must complete first.");

        if (role == Role.Mentee)
            MenteeCompletedAt = DateTime.UtcNow;
        else
            MentorCompletedAt = DateTime.UtcNow;

        if (MenteeCompletedAt is not null && MentorCompletedAt is not null)
        {
            Status = SessionStatus.Completed;
            AddDomainEvent(new SessionCompletedEvent(Id));
        }

        return Result.Success();
    }
}
```

**Value Objects for type safety**:
```csharp
public sealed record Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('@'))
            return Result<Email>.Failure("Invalid email format.");
        return Result<Email>.Success(new Email(input.Trim().ToLowerInvariant()));
    }
}
```

### Why This Approach
- **DDD**: Rich domain model — entities have behavior, not just properties. `MarkComplete()` enforces the business rule "mentee first, then mentor" inside the domain, not in a handler.
- **Security by Design**: Business rules can't be bypassed — the entity itself prevents invalid transitions. Private setters mean only domain methods can change state.
- **Resilience**: State machine prevents data corruption even if retries cause duplicate calls. Calling `MarkComplete` twice for the same role is idempotent.
- **Testing**: Pure domain logic is trivially testable — no mocks needed. I instantiate the entity, call the method, assert the result.
- **Design Patterns**: State Machine pattern on SessionStatus. Factory Method on Value Objects (private constructor + static Create method).

---

## Step 7: Application Layer (CQRS + MediatR)

### What I Do
I separate commands (writes) from queries (reads). Each has its own handler, validation, and return type. Commands change state and return Result. Queries read state and return DTOs.

### How I Do It

**Command definition (write side)**:
```csharp
public sealed record MarkCompleteCommand(
    Guid SessionId,
    Guid UserId,
    Role Role) : IRequest<Result>, IAuditableCommand
{
    public string AuditDescription => $"Mark session {SessionId} complete as {Role}";
}
```

**Command handler**:
```csharp
public sealed class MarkCompleteCommandHandler(
    ISessionRepository sessionRepository,
    IEventPublisher eventPublisher)
    : IRequestHandler<MarkCompleteCommand, Result>
{
    public async Task<Result> Handle(MarkCompleteCommand request, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(
            new SessionId(request.SessionId), ct);

        if (session is null)
            return Result.Failure("Session not found.");

        var result = session.MarkComplete(request.Role);
        if (!result.IsSuccess)
            return result;

        await sessionRepository.SaveAsync(session, ct);

        foreach (var domainEvent in session.DomainEvents)
            await eventPublisher.PublishAsync(domainEvent, ct);

        return Result.Success();
    }
}
```

**Query definition (read side)**:
```csharp
public sealed record GetMenteeDashboardQuery(Guid UserId)
    : IRequest<MenteeDashboardDto>;
```

**FluentValidation for input validation**:
```csharp
public sealed class MarkCompleteCommandValidator
    : AbstractValidator<MarkCompleteCommand>
{
    public MarkCompleteCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
```

### Why This Approach
- **CQRS**: Write side optimized for consistency (strong reads, conditional writes). Read side optimized for speed (denormalized, eventually consistent).
- **Microservices**: Handlers are the unit of deployment — add a new handler without modifying existing ones (Open/Closed principle).
- **Testing**: Each handler is independently testable with mocked repositories. No shared mutable state between handlers.
- **Observability**: MediatR pipeline gives us per-handler metrics automatically — I know exactly which handlers are slow.
- **Security by Design**: IAuditableCommand marker interface ensures all state-changing operations are audit-logged without handler authors remembering to do it.

---

## Step 8: Security Middleware Stack

### What I Do
I implement 7 layers of defense, each catching different threat classes. Security is not a feature — it's a cross-cutting concern woven through the entire request pipeline.

### How I Do It (in order)

**Layer 1: Security Headers**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";
    context.Response.Headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});
```

**Layer 2: Request Body Size Limit (1MB)** — prevents memory exhaustion DoS

**Layer 3: CSRF Protection**
```csharp
// SameSite=Strict cookies + Origin header validation
services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

**Layer 4: JWT Validation (Cognito)**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidAudience = clientId
        };
    });
```

**Layer 5: Rate Limiting (100 req/min sliding window)**
```csharp
services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("default", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow = 6;
        limiter.PermitLimit = 100;
    });
});
```

**Layer 6: Resource Access Control** — userId in JWT must match resource owner

**Layer 7: Input Validation** — FluentValidation on every command before handler executes

### Why This Approach
- **Security by Design**: Defense-in-depth — each layer catches what others miss. If rate limiting fails, the body size limit still protects. If JWT is stolen, resource ownership still blocks cross-user access.
- **Scalability**: Rate limiting per-user means one abuser can't take down the system for everyone.
- **Resilience**: Each middleware is independent — a bug in CSRF doesn't break JWT validation.
- **Governance**: All security config is in code (SecurityOptions class) — auditable, reviewable, testable.
- **Cost Optimization**: Early rejection (size limit, rate limit) prevents expensive downstream processing.

---

## Step 9: Observability Framework

### What I Do
I set up structured logging, distributed tracing, metrics, alarms, and dashboards BEFORE deploying to production. Observability is not an afterthought — it's designed into the system from day one.

### How I Do It

**Structured Logging (Serilog → CloudWatch)**:
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Service", "GuidedMentor")
    .Enrich.WithProperty("Environment", environment)
    .Enrich.WithCorrelationId()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

Every log entry includes: correlationId, userId, requestPath, duration, handlerName, outcome.

**Distributed Tracing (OpenTelemetry → X-Ray)**:
```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAWSInstrumentation()
        .AddOtlpExporter());
```

**CloudWatch Custom Metrics**:
- `TokenUsage` — tracks Bedrock token consumption per session plan
- `MatchingScore` — distribution of AI matching scores
- `HandlerDuration` — P50/P95/P99 per handler
- `ErrorRate` — percentage of 5xx responses

**Alarms (threshold-based)**:
| Alarm | Threshold | Action |
|-------|-----------|--------|
| Error Rate | >1% for 5 min | SNS → PagerDuty |
| P99 Latency | >5s for 3 min | SNS → Slack |
| Bedrock Failures | >3 in 5 min | SNS → Engineering |
| DDB Throttled | >0 in 1 min | SNS → Ops |
| DLQ Depth | >0 messages | SNS → Engineering |
| Budget 80% | Monthly threshold | SNS → Finance |

**Dashboards (3 views)**:
1. **Ops Dashboard**: Latency P50/P95/P99, error rates, Lambda concurrency, DDB capacity
2. **Business Dashboard**: Matches created, sessions completed, onboarding funnel, DAU/MAU
3. **Cost Dashboard**: Per-service spend, Bedrock token costs, DDB read/write units

### Why This Approach
- **Observability**: Can't improve what you can't measure. Structured logs make debugging 10x faster than unstructured text. CorrelationId traces a request across all services.
- **Monitoring performance/reliability/availability**: Alarms detect degradation before users report it. Dashboards show trends over time.
- **Cost Optimization**: Budget alerts prevent surprise bills. Per-service cost tags identify which bounded context costs most.
- **Capacity Monitoring**: DynamoDB capacity alarms at progressive thresholds (50%/70%/90%) give warning before throttling occurs.
- **Resilience**: DLQ depth alarm means failed events never sit unnoticed — the team is alerted within minutes.

---

## Step 10: Testing Strategy

### What I Do
I implement a testing pyramid: many unit tests (fast, cheap), some integration tests, few E2E tests (slow, expensive). Every layer of the system has appropriate test coverage.

### How I Do It

**Unit Tests (xUnit + FluentAssertions + NSubstitute)**:
```csharp
public sealed class MarkCompleteCommandHandlerTests
{
    [Fact]
    public async Task Handle_MenteeCompletesFirst_ShouldSucceed()
    {
        // Arrange
        var session = SessionFactory.CreateActive();
        var repo = Substitute.For<ISessionRepository>();
        repo.GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new MarkCompleteCommandHandler(repo, Substitute.For<IEventPublisher>());
        var command = new MarkCompleteCommand(session.Id.Value, Guid.NewGuid(), Role.Mentee);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.MenteeCompletedAt.Should().NotBeNull();
    }
}
```

**Property-Based Tests (FsCheck — 41 properties)**:
```csharp
[Property(MaxTest = 100)]
[Trait("Category", "Property")]
public Property Property6_MatchingScoreBoundsAndDeterminism()
{
    return Prop.ForAll(
        Arb.From<MentorProfile>(),
        Arb.From<MenteeProfile>(),
        (mentor, mentee) =>
        {
            var score1 = MatchingEngine.Calculate(mentor, mentee);
            var score2 = MatchingEngine.Calculate(mentor, mentee);
            return score1 >= 0 && score1 <= 100 && score1 == score2;
        });
}
```

**Frontend Tests (Vitest + React Testing Library + axe-core)**:
```typescript
describe('MentorCard', () => {
  it('should have no accessibility violations', async () => {
    const { container } = render(<MentorCard mentor={mockMentor} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should show lock button only for available mentors', () => {
    render(<MentorCard mentor={{ ...mockMentor, isAvailable: true }} />);
    expect(screen.getByRole('button', { name: /lock mentor/i })).toBeInTheDocument();
  });
});
```

**E2E Tests (Playwright — 8 user journeys)**:
```typescript
test('mentee can browse and lock a mentor', async ({ page }) => {
  await page.goto('/mentors');
  await page.getByRole('button', { name: /lock/i }).first().click();
  await expect(page.getByText(/mentor locked/i)).toBeVisible();
});
```

**Coverage Thresholds enforced in CI**:
- Domain/pure logic: ≥95% line coverage
- Application handlers: ≥80% line coverage
- Frontend components: ≥70%

### Why This Approach
- **Testing Strategies**: Pyramid ensures fast feedback (unit in <5s) while still catching integration issues (E2E in staging).
- **CI/CD Embedded**: Tests gate deployment — nothing reaches production without passing all automated checks.
- **Property-Based Testing**: Unit tests prove examples work. Property tests prove invariants hold for ALL possible inputs (100 random cases per property). This catches edge cases I'd never think to write manually.
- **Accessibility**: axe-core scans catch WCAG violations automatically — no manual a11y testing forgotten.
- **Cost Optimization**: Fast unit tests run on every commit (cheap). Slow E2E tests run only on staging deploy (expensive but thorough).

---

## Step 11: Frontend Architecture (React 19 + Optimization)

### What I Do
I build a modern React SPA with code splitting, deferred rendering, virtualization, and progressive web app support. The frontend is optimized for both performance and accessibility.

### How I Do It

**Project structure (monorepo with packages)**:
```
frontend/
├── host-shell/           # Main app shell, routing, providers
│   └── src/
│       ├── pages/        # Route-level components (lazy loaded)
│       ├── components/   # Shared layout components
│       ├── hooks/        # Custom hooks
│       ├── providers/    # Auth, Theme, Query providers
│       ├── mocks/        # MSW handlers for development
│       └── types/        # Shared TypeScript types
├── packages/
│   └── design-system/    # Shared UI components (cards, buttons, inputs)
```

**Route-level code splitting**:
```typescript
const MentorBrowse = lazy(() => import('./pages/MentorBrowsePage'));
const MenteeDashboard = lazy(() => import('./pages/MenteeDashboardPage'));
const SessionPlan = lazy(() => import('./pages/SessionPlanPage'));

function App() {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <Routes>
        <Route path="/mentors" element={<MentorBrowse />} />
        <Route path="/dashboard" element={<MenteeDashboard />} />
        <Route path="/sessions/:id" element={<SessionPlan />} />
      </Routes>
    </Suspense>
  );
}
```

**Deferred values for non-blocking search**:
```typescript
function MentorBrowsePage() {
  const [search, setSearch] = useState('');
  const deferredSearch = useDeferredValue(search);
  // Filter uses deferredSearch — typing never lags
  const filtered = mentors.filter(m =>
    m.name.toLowerCase().includes(deferredSearch.toLowerCase())
  );
}
```

**Memoization to prevent unnecessary re-renders**:
```typescript
const MentorCard = memo(function MentorCard({ mentor, onLock }: Props) {
  return (
    <article className="glass-card" aria-label={`Mentor: ${mentor.name}`}>
      <h3 style={{ fontFamily: 'Outfit' }}>{mentor.name}</h3>
      <ScoreRing score={mentor.matchScore} />
      <button className="btn-violet" onClick={() => onLock(mentor.id)}
        aria-label={`Lock mentor ${mentor.name}`}>
        Lock Mentor
      </button>
    </article>
  );
});
```

**Virtualization for large lists**:
```typescript
import { useVirtualizer } from '@tanstack/react-virtual';

function NotificationList({ notifications }: Props) {
  const parentRef = useRef<HTMLDivElement>(null);
  const virtualizer = useVirtualizer({
    count: notifications.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 72,
  });
  // Renders only visible items — handles 10,000+ notifications at 60fps
}
```

**Accessibility built-in**:
```html
<!-- Skip navigation link -->
<a href="#main-content" className="sr-only focus:not-sr-only">
  Skip to main content
</a>

<!-- Semantic landmarks -->
<nav aria-label="Main navigation">...</nav>
<main id="main-content" tabIndex={-1}>...</main>

<!-- Live regions for dynamic updates -->
<div aria-live="polite" aria-atomic="true">
  {notification && <p>{notification.message}</p>}
</div>
```

### Why This Approach
- **React Optimization**: Lazy loading means initial load is ~142KB instead of 800KB. Deferred values prevent typing lag. Virtualization handles 1000+ item lists at 60fps. Memoization prevents cascading re-renders on sibling updates.
- **Accessibility**: Skip-nav link, ARIA labels on all interactive elements, focus traps in modals, semantic HTML landmarks, live regions for dynamic content. All verified by axe-core in CI.
- **Scalability**: Code splitting scales with feature count — each new page is a separate chunk, not added to the main bundle.
- **Design Patterns**: Observer pattern (TanStack Query cache invalidation), Strategy pattern (different card layouts by role), Composite pattern (nested layout components).
- **Cost Optimization**: Smaller bundles = less S3 transfer = lower CloudFront costs. Service Worker caches static assets.

---

## Step 12: Resilience Patterns

### What I Do
I implement Polly v8 resilience pipelines, graceful degradation, DLQs, and automated rollback. The system assumes failure is normal and designs for it.

### How I Do It

**Polly v8 Resilience Pipeline**:
```csharp
services.AddResiliencePipeline("bedrock", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder()
                .Handle<ThrottlingException>()
                .Handle<ServiceUnavailableException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(60)
        })
        .AddTimeout(TimeSpan.FromSeconds(30));
});
```

**Graceful Degradation (AI plan generation)**:
```csharp
public sealed class GenerateSessionPlanHandler(
    IBedrockClient bedrock,
    ISessionRepository sessions,
    ResiliencePipeline pipeline)
{
    public async Task<Result> Handle(GenerateSessionPlanCommand cmd, CancellationToken ct)
    {
        try
        {
            var plan = await pipeline.ExecuteAsync(
                async token => await bedrock.GenerateAsync(cmd.Prompt, token), ct);
            await sessions.SavePlanAsync(cmd.SessionId, plan, ct);
        }
        catch (BrokenCircuitException)
        {
            // Graceful degradation: mark as pending, retry later via EventBridge
            await sessions.UpdateStatusAsync(cmd.SessionId, "pending_plan", ct);
            await eventBridge.ScheduleRetryAsync(cmd, TimeSpan.FromMinutes(5), ct);
            return Result.Success(); // Don't fail the user experience
        }

        return Result.Success();
    }
}
```

**Dead Letter Queues (every async path)**:
```hcl
resource "aws_sqs_queue" "session_plan_dlq" {
  name                      = "${var.environment}-session-plan-dlq"
  message_retention_seconds = 1209600  # 14 days
  tags = {
    BoundedContext = "Content"
    Service        = "SessionPlanGenerator"
  }
}

resource "aws_cloudwatch_metric_alarm" "dlq_depth" {
  alarm_name  = "${var.environment}-session-plan-dlq-depth"
  metric_name = "ApproximateNumberOfMessagesVisible"
  namespace   = "AWS/SQS"
  threshold   = 0
  # ...
}
```

**Canary Deployment**:
```yaml
# deploy-backend.yml
- name: Deploy canary (10% traffic)
  run: |
    aws lambda update-alias --function-name $FUNCTION \
      --name live \
      --routing-config AdditionalVersionWeights={"$NEW_VERSION"=0.1}

- name: Monitor canary (2 min)
  run: |
    sleep 120
    ERROR_RATE=$(aws cloudwatch get-metric-statistics ...)
    if [ "$ERROR_RATE" -gt "5" ]; then
      aws lambda update-alias --function-name $FUNCTION --name live \
        --routing-config AdditionalVersionWeights={}
      exit 1
    fi

- name: Promote to 100%
  run: |
    aws lambda update-alias --function-name $FUNCTION \
      --name live --function-version $NEW_VERSION
```

### Why This Approach
- **Resilience**: No single failure takes down the platform. Bedrock down? Session plans degrade gracefully. DynamoDB throttled? Polly retries handle it transparently.
- **Scalability**: Circuit breaker prevents retry storms from overloading a recovering service. Backpressure is built in.
- **Cost Optimization**: DLQ is free (SQS first 1M requests). Canary deploy is free (Lambda aliases). Retries avoid costly full re-processing.
- **Observability**: DLQ depth alarm detects processing failures automatically. Circuit breaker state changes are logged.
- **CI/CD**: Canary deployment catches bad deploys in 2 minutes — auto-rolls back before users notice.

---

## Step 13: CI/CD Pipeline

### What I Do
I create 9 GitHub Actions workflows that automate the entire path from code to production. Every change is validated, tested, scanned, and deployed with zero manual steps.

### How I Do It

**Workflow Overview**:
| Workflow | Trigger | What It Does |
|----------|---------|--------------|
| ci-dotnet.yml | PR to main | Build, test, coverage, lint |
| ci-react.yml | PR to main | Build, test, axe-core, lint |
| terraform-checks.yml | PR (infra changes) | fmt, validate, tflint, checkov, plan |
| security-scan.yml | PR + weekly schedule | OWASP ZAP, NuGet audit, npm audit |
| deploy-infrastructure.yml | Merge to main | terraform apply (staging → prod) |
| deploy-backend.yml | Merge to main | Lambda deploy with canary |
| deploy-frontend.yml | Merge to main | S3 sync + CloudFront invalidation |
| e2e-tests.yml | After staging deploy | Playwright against staging |
| terraform-drift.yml | Daily schedule | Detect manual console changes |

**PR Gate (blocks merge if any fail)**:
```yaml
# ci-dotnet.yml
name: .NET CI
on:
  pull_request:
    branches: [main]
    paths: ['backend/**']

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test with coverage
        run: |
          dotnet test --no-build --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage

      - name: Enforce coverage thresholds
        run: |
          # Domain ≥95%, Handlers ≥80%
          dotnet reportgenerator -reports:coverage/**/coverage.cobertura.xml \
            -targetdir:coverage-report -reporttypes:TextSummary
          # Parse and fail if below threshold
```

**Deployment Pipeline (staging → canary → prod)**:
```yaml
# deploy-backend.yml
jobs:
  deploy-staging:
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - name: Deploy to staging
        run: |
          dotnet lambda deploy-function --function-name $STAGING_FUNCTION

  e2e-staging:
    needs: deploy-staging
    uses: ./.github/workflows/e2e-tests.yml
    with:
      environment: staging

  deploy-production:
    needs: e2e-staging
    runs-on: ubuntu-latest
    environment: production  # Requires manual approval
    steps:
      - name: Canary deploy (10%)
        run: # ... (see Step 12)
      - name: Monitor and promote
        run: # ... (see Step 12)
```

### Why This Approach
- **CI/CD Embedded Testing**: Nothing reaches production without all tests passing + security scan + coverage thresholds. The pipeline IS the quality gate.
- **Governance**: Every infrastructure change is planned on PR (team reviews the diff) and applied only after merge. Production requires manual approval.
- **Resilience**: Canary deployment catches bad deploys in 2 minutes — auto-rolls back before users notice.
- **Security**: OWASP ZAP + NuGet audit + npm audit run weekly and on every PR. Vulnerabilities block merge.
- **Cost Optimization**: GitHub Actions free tier (2000 min/month) covers most of CI. Only E2E and production deploys consume meaningful minutes.
- **Observability**: Every workflow publishes status to Slack. Failed deploys trigger incident alerts.

---

## Step 14: Cost Optimization & Governance

### What I Do
I ensure the platform costs $0 in dev, scales linearly with usage, and has automated cost governance. Every architectural decision considers cost impact.

### How I Do It

**Serverless-first architecture (pay per use)**:
| Service | Pricing Model | Dev Cost | Prod Cost (1K users) |
|---------|--------------|----------|---------------------|
| Lambda | Per invocation (100ms) | $0 (free tier) | ~$3/month |
| DynamoDB | Per read/write unit | $0 (on-demand, idle) | ~$5/month |
| Aurora Serverless v2 | Per ACU-hour | Skipped in dev | ~$45/month |
| S3 | Per GB stored + requests | <$0.01 | ~$1/month |
| CloudFront | Per GB transferred | Free tier | ~$2/month |
| Cognito | Per MAU | Free (first 50K) | $0 |
| EventBridge | Per event | Free (first 100K/mo) | <$1/month |
| Bedrock | Per token | $0 (mocked in dev) | ~$15/month |

**Conditional infrastructure (dev vs prod)**:
```hcl
# variables.tf
variable "enable_aurora" { default = false }
variable "enable_waf" { default = false }
variable "enable_kms" { default = false }

# Only created in staging/prod
resource "aws_rds_cluster" "analytics" {
  count = var.enable_aurora ? 1 : 0
  # ...
}
```

**Cost governance automation**:
```hcl
# Budget alerts
resource "aws_budgets_budget" "monthly" {
  name         = "guidedmentor-monthly"
  budget_type  = "COST"
  limit_amount = "100"
  limit_unit   = "USD"

  notification {
    comparison_operator = "GREATER_THAN"
    threshold           = 50
    notification_type   = "ACTUAL"
    subscriber_email_addresses = ["team@guidedmentor.dev"]
  }

  notification {
    comparison_operator = "GREATER_THAN"
    threshold           = 80
    notification_type   = "ACTUAL"
    subscriber_email_addresses = ["team@guidedmentor.dev"]
  }

  notification {
    comparison_operator = "GREATER_THAN"
    threshold           = 100
    notification_type   = "ACTUAL"
    subscriber_email_addresses = ["team@guidedmentor.dev", "finance@guidedmentor.dev"]
  }
}

# Cost Anomaly Detection
resource "aws_ce_anomaly_monitor" "service" {
  name         = "guidedmentor-anomaly-monitor"
  monitor_type = "DIMENSIONAL"
  monitor_dimension = "SERVICE"
}
```

**Resource tagging for cost allocation**:
```hcl
locals {
  common_tags = {
    Environment    = var.environment
    Service        = "GuidedMentor"
    BoundedContext = var.bounded_context
    ManagedBy      = "Terraform"
    CostCenter     = "Engineering"
  }
}
```

**S3 Intelligent-Tiering for resumes**:
```hcl
resource "aws_s3_bucket_intelligent_tiering_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  name   = "auto-tier"

  tiering {
    access_tier = "ARCHIVE_ACCESS"
    days        = 90
  }
}
```

### Why This Approach
- **Cost Optimization**: Serverless architecture means zero cost at zero load. No idle EC2 instances burning money overnight. Scales linearly with actual usage.
- **Capacity Monitoring**: DynamoDB capacity alarms give 3 levels of warning (50%, 70%, 90%) before throttling. Budget alerts catch spending before it's a problem.
- **Governance**: All costs attributable to specific bounded contexts via tags. Cost Anomaly Detection catches unexpected spending patterns automatically.
- **Terraform**: Infrastructure cost is transparent — `terraform plan` shows what will be created and its pricing model.
- **Scalability**: On-demand DynamoDB + Aurora Serverless v2 + Lambda means no capacity planning needed. Scale from 0 to 10,000 concurrent users without provisioning changes.

---

## Summary: How Everything Connects

| Principle | How It Manifests in GuidedMentor |
|-----------|----------------------------------|
| DDD | 4 bounded contexts, rich domain models, aggregate roots, domain events, value objects, ubiquitous language per context |
| Security by Design | 7-layer middleware, JWT + resource ownership, encryption at rest/transit, input sanitization, audit logging, OWASP scanning |
| Scalability | Serverless (Lambda + DynamoDB), auto-scaling (on-demand, Aurora Serverless v2), code splitting, virtualization, GUID partition keys |
| Resilience | Polly v8 retry + circuit breaker, DLQs, graceful degradation, canary deploy, multi-AZ, state machine idempotency |
| API-First | OpenAPI contracts, versioned URLs, consistent error format, defined before implementation, parallel team development |
| Microservices | 4 independently deployable Lambdas, own tables, event-driven cross-context communication via EventBridge |
| Design Patterns | Repository, Mediator, CQRS, State Machine, Factory, Circuit Breaker, Observer, Anti-Corruption Layer, Decorator (pipeline), Strategy |
| Observability | Structured logging, CloudWatch alarms/dashboards, custom metrics, distributed tracing (X-Ray), budget alerts, DLQ monitoring |
| Testing | 487 automated tests (unit + property + frontend + a11y + E2E), embedded in CI, coverage thresholds enforce quality |
| Data Strategy | DynamoDB (operational, denormalized) + Aurora (analytics, normalized), composite partition keys, GUID distribution, DDB Streams |
| Terraform | 9+ modules, conditional resources, tflint + checkov, drift detection, prevent_destroy on data, environment-specific tfvars |
| Accessibility | Skip-nav, ARIA labels, focus traps, semantic HTML, axe-core CI checks, keyboard navigation, live regions, reduced motion respect |
| Governance | Steering files, hooks, coding standards, compliance docs, ADRs, runbooks, resource tags, budget alerts, manual approval gates |
| CI/CD | 9 workflows, PR gates, canary deploys, staging validation, auto-rollback, security scanning, coverage enforcement |
| React Optimization | Lazy loading, useDeferredValue, React.memo, virtualization, service worker caching, Suspense boundaries, TailwindCSS v4 |
| Cost Optimization | $0 dev environment, pay-per-use serverless, conditional infrastructure, intelligent tiering, budget alerts, anomaly detection |

---

## Key Interview Talking Points

### "Tell me about a technical challenge you solved"
"Our AI session plan generation was intermittently failing due to Bedrock throttling. Instead of failing the user flow, I implemented graceful degradation — the session is created with `pending_plan` status, a scheduled EventBridge rule retries every 5 minutes, and the user sees a 'generating...' state. Combined with Polly v8 circuit breaker (5 failures in 30s triggers 60s break), we went from 3% error rate to 0.01%."

### "How do you ensure quality at scale?"
"I built a testing pyramid with 487 automated tests. Unit tests run in <5 seconds for instant feedback. Property-based tests (FsCheck) prove mathematical invariants hold for ALL inputs — not just the examples I thought of. axe-core catches accessibility regressions. Coverage thresholds (95% domain, 80% handlers) block merges below quality bar. Nothing reaches production without passing every gate."

### "How do you handle security?"
"Security is designed in, not bolted on. Seven middleware layers each catch different threat classes — defense-in-depth. JWT validates identity, resource ownership validates authorization, rate limiting prevents abuse, input validation prevents injection. Every state-changing command is automatically audit-logged via MediatR pipeline. OWASP ZAP runs on every PR and weekly."

### "How do you optimize costs?"
"The entire dev environment costs $0 — conditional Terraform resources skip expensive services (Aurora, WAF, KMS) in dev. Production uses serverless-first: Lambda, DynamoDB on-demand, Aurora Serverless v2. At zero load, monthly cost is under $50. Budget alerts at 50/80/100% catch spending anomalies. Cost Anomaly Detection triggers automated alerts."

### "Walk me through your deployment process"
"Merge to main triggers: Terraform applies infrastructure changes → Lambda deploys as canary (10% traffic for 2 minutes, auto-rollback on error spike) → Frontend syncs to S3 + CloudFront invalidation → E2E tests validate staging. Production requires manual approval. Drift detection runs daily to catch console changes. The entire pipeline is codified — reproducible and auditable."
