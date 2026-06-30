# Implementation Plan: AWS Community GuidedMentor Platform

## Overview

This implementation plan covers the full-stack build of the GuidedMentor AI-powered mentorship platform using .NET 10 (C#) Native AOT Lambda microservices, React 19.2 Module Federation micro-frontends, DynamoDB, Aurora PostgreSQL, and Terraform. Tasks are organised into 8 phases following DDD bounded contexts, with property-based tests (FsCheck) validating all 35 correctness properties.

## Tasks

- [x] 1. Phase 1 — Foundation (Project Structure, Shared Libraries, Design System)

  - [x] 1.1 Initialise monorepo structure and solution files
    - Create .NET solution (`GuidedMentor.sln`) with folder structure per bounded context (Identity, Mentoring, Content, Engagement)
    - Each context gets Domain, Application, Infrastructure, Api projects (Clean Architecture)
    - Add `tools/SeedData` console project placeholder
    - Configure Directory.Build.props for .NET 10, Native AOT, nullable, implicit usings
    - _Requirements: 15.1, 15.4, 15.5_

  - [x] 1.2 Create shared Domain kernel and cross-cutting packages
    - Implement `GuidedMentor.SharedKernel`: Entity<T>, AggregateRoot<T>, ValueObject base classes, Result<T> type, IRepository<T> interface
    - Implement shared enums: Role, AustralianChapter, OnboardingStatus, ExperienceLevel, PrimaryGoal, NotificationType, SessionStatus, EmploymentType, AvailabilityStatus
    - Implement shared value objects: Email, UserId, MentorId, MenteeId, SessionId, etc.
    - _Requirements: 15.5, 27.1, 27.2_

  - [x] 1.3 Configure MediatR, FluentValidation, and Polly v8 shared infrastructure
    - Add MediatR pipeline behaviors: ValidationBehavior, LoggingBehavior, PerformanceBehavior
    - Configure Polly v8 resilience pipelines (bedrock, dynamodb, aurora) as shown in design
    - Add FluentValidation auto-discovery registration
    - _Requirements: 15.5, 15.7, 24.5, 24.6, 24.7_

  - [x] 1.4 Set up Serilog structured logging and OpenTelemetry tracing
    - Configure Serilog JSON sink to CloudWatch with enrichers (correlationId, userId, requestPath, duration, service, environment)
    - Configure OpenTelemetry SDK with X-Ray exporter and custom metrics
    - Implement correlation ID middleware (X-Correlation-Id header propagation)
    - _Requirements: 22.1, 22.2, 22.3, 15.10_

  - [x] 1.5 Set up React 19.2 Module Federation host shell and remote scaffolds
    - Create Vite 6 host shell (port 3000) with Module Federation plugin, React Router v7, TanStack Query provider
    - Scaffold 4 remote apps: identity (3001), mentoring (3002), content (3003), engagement (3004)
    - Configure shared dependencies (react, react-dom, react-router-dom, @tanstack/react-query)
    - Implement ErrorBoundary for Module Federation fallbacks
    - _Requirements: 18.1, 18.2_

  - [x] 1.6 Implement design system tokens, glassmorphism utilities, and shared UI components
    - Create CSS custom properties (design tokens from design.md: colours, spacing, typography, shadows, z-index)
    - Implement TailwindCSS 4 configuration with custom theme extending design tokens
    - Create glassmorphism utility classes (.glass-card) with backdrop-filter fallback
    - Implement shared components: Button, Input, Modal, Toast, Skeleton, ConfirmDialog, ProgressIndicator
    - Add prefers-reduced-motion media query support
    - _Requirements: 18.3, 18.4, 18.7, 25.11_

  - [x] 1.7 Implement accessibility foundations (skip-nav, landmarks, ARIA helpers, focus trap)
    - Create SkipNavLink component (first focusable element on every page)
    - Implement focus trap utility for modals and wizards
    - Create aria-live announcement utility (for dynamic content: errors, notifications, loading states)
    - Implement landmark roles (main, nav, aside, footer) in host shell layout
    - _Requirements: 18.5, 18.6, 18.8, 25.12_

  - [x] 1.8 Set up testing infrastructure (xUnit, FsCheck, Vitest, axe-core, Playwright scaffold)
    - Configure xUnit test projects per bounded context with FsCheck 3.x integration
    - Configure PropertyTestBase with 100 iterations minimum
    - Set up Bogus library for realistic Australian test data (names, emails, chapters)
    - Configure Vitest + React Testing Library for frontend component tests
    - Add axe-core integration for CI accessibility checks (block < 90 score)
    - Scaffold Playwright E2E project
    - _Requirements: 23.3, 27.3, 27.4_

  - [x] 1.9 Implement global exception handler and structured error response model
    - Create ApiErrorResponse DTO (statusCode, error, message, correlationId, fieldErrors)
    - Implement GlobalExceptionHandler middleware mapping exception types to HTTP status codes
    - Implement frontend handleApiError utility with status-code-based routing (validation → inline, 401 → refresh/redirect, 429 → countdown, 5xx → retry toast)
    - _Requirements: 15.6, 15.7, 25.9, 25.10_

  - [ ]* 1.10 Write property test for architecture layer dependencies (Property 20)
    - **Property 20: Architecture Layer Dependencies Follow Clean Architecture**
    - Implement NetArchTest tests: Domain has zero dependencies on Infrastructure/Application/Presentation; Application has zero dependencies on Infrastructure/Presentation; only Infrastructure references AWS SDK packages
    - **Validates: Requirements 27.1**

- [x] 2. Checkpoint — Foundation complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Phase 2 — Data Layer (Terraform Infrastructure)

  - [x] 3.1 Create Terraform backend configuration and module structure
    - Set up S3 remote state + DynamoDB lock table (backend.tf)
    - Create module skeleton: identity/, mentoring/, content/, engagement/, networking/, analytics/, observability/
    - Create environment tfvars: dev.tfvars, staging.tfvars, prod.tfvars
    - Implement environment-conditional logic (dev: skip Aurora/WAF/Guardrails/CMK/RDS Proxy; staging/prod: all enabled)
    - _Requirements: 23.1_

  - [x] 3.2 Implement Identity Terraform module (Cognito + Users table)
    - Create Cognito User Pool with Google OAuth provider, email/password, 12-char password policy, MFA enforcement
    - Create Users DynamoDB table with GSI-Email, on-demand capacity, PITR enabled
    - Configure Cognito app client with JWT 15-min access + 7-day refresh tokens
    - Environment-conditional: CMK encryption (staging/prod only, AWS-managed in dev)
    - _Requirements: 1.1, 1.2, 16.1, 16.2, 16.8, 21.1, 21.13_

  - [x] 3.3 Implement Mentoring Terraform module (Mentors, Mentees, Sessions, Jobs tables)
    - Create Mentors table with GSI-UserId, GSI-Available, on-demand, PITR
    - Create Mentees table with GSI-UserId, on-demand, PITR
    - Create Sessions table with GSI-Mentee, GSI-Mentor, TTL on lockExpiresAt, on-demand, PITR
    - Create Jobs table with GSI-Mentor, GSI-Status, on-demand, PITR
    - Environment-conditional: CMK encryption (staging/prod only)
    - _Requirements: 16.1, 16.3, 16.4, 16.5, 16.8, 21.8, 21.13_

  - [x] 3.4 Implement Engagement Terraform module (Notifications, Meetups, EngagementEvents tables + AppSync)
    - Create Notifications table with GSI-Recipient (PK=recipientUserId, SK=createdAt), on-demand, PITR
    - Create Meetups table with GSI-Chapter (PK=chapter, SK=eventDate), on-demand
    - Create EngagementEvents table with GSI-User (PK=userIdHash, SK=timestamp), TTL (90 days), on-demand
    - Configure AppSync GraphQL API with Cognito authorization for real-time subscriptions
    - _Requirements: 16.1, 16.6, 16.8, 12.1_

  - [x] 3.5 Implement Networking Terraform module (API Gateway, CloudFront, S3)
    - Create API Gateway REST API with /v1/ prefix, Cognito JWT authorizer, usage plan (100 req/min/user)
    - Create S3 buckets: SPA assets (CloudFront origin) + Resume storage (SSE-AES256, versioning, no public access)
    - Create CloudFront distribution with OAI, HTTPS only, security headers (CSP, HSTS, X-Frame-Options)
    - Configure CORS (allow guidedmentor.dev + localhost)
    - Environment-conditional: WAF (dormant in dev, active staging/prod)
    - _Requirements: 15.1, 15.2, 15.8, 15.11, 19.3, 19.6, 19.7, 21.4, 21.5, 21.12_

  - [x] 3.6 Implement Analytics Terraform module (Aurora PostgreSQL + RDS Proxy + DDB Streams)
    - Create Aurora PostgreSQL Serverless v2 cluster (0.5-8 ACUs, multi-AZ, 35-day backup, KMS)
    - Create RDS Proxy for Lambda → Aurora connection pooling (staging/prod only; skipped in dev)
    - Configure DynamoDB Streams on all application tables for Aurora replication
    - Create analytics schema (matches, sessions, users, engagement_metrics tables + indexes + views)
    - Environment-conditional: Aurora dormant/skipped in dev; active in staging/prod
    - _Requirements: 16.7, 24.1, 24.4, 26.1, 26.4_

  - [x] 3.7 Implement Observability Terraform module (CloudWatch dashboards, alarms, budgets)
    - Create CloudWatch dashboards: Ops (latency, errors, capacity), Business (matches, sessions), Cost (per-service)
    - Create alarms: error >1% (5min), p99 >5s, Bedrock failures >3 (10min), DDB throttled >0
    - Create SNS topic for alarm notifications
    - Create AWS Budget alerts (50%, 80%, 100% monthly threshold)
    - Tag all resources with cost allocation tags (Environment, Service, BoundedContext)
    - _Requirements: 22.4, 22.5, 22.7, 22.8_

  - [x] 3.8 Implement Security Terraform resources (WAF, KMS CMK, IAM permission boundaries)
    - Create KMS CMK with annual rotation for customer-sensitive data (staging/prod only)
    - Create WAF Web ACL: Common Rule Set, rate-based (2000/5min), bot control, geo-restrict (AU only)
    - Create Lambda execution roles with permission boundaries and explicit deny cross-account
    - Configure Lambda code signing (staging/prod only)
    - Environment-conditional: WAF/CMK dormant in dev, full in staging/prod
    - _Requirements: 21.12, 21.13, 21.14, 21.15_

  - [x]* 3.9 Write property test for KMS encryption on all customer-sensitive tables (Property 19)
    - **Property 19: KMS Encryption Key Is Used for All Customer-Sensitive Tables**
    - Verify via Terraform plan output or integration test: Users, Mentors, Mentees, Sessions tables + S3 resumes + Aurora all reference CMK ARN (not default)
    - **Validates: Requirements 21.13**

  - [x] 3.9.1 Implement EventBridge resources (event bus, scheduler, rules, DLQs)
    - Create EventBridge event bus for cross-context async operations
    - Create EventBridge Scheduler rules: lock expiration cleanup (5min), analytics aggregation (hourly), notification digest (daily 9AM AEST), availability reminder (daily)
    - Create SQS dead-letter queues for all EventBridge rule targets (14-day retention)
    - _Requirements: 20.1, 20.6, 20.7_

- [x] 4. Checkpoint — Data layer and infrastructure complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Phase 3 — Identity Context (Auth, Role Toggle, Onboarding, Super Admin)

  - [x] 5.1 Implement User domain model and role management
    - Create User entity with Email, ActiveRole, DisplayName, ProfilePhotoUrl, AwsChapter, City, OnboardingStatus (mentor/mentee), FailedLoginAttempts, LockedUntil
    - Implement ToggleRole() domain logic (switch to opposite role)
    - Implement IncrementFailedAttempts() with 5-in-15min lockout logic
    - Implement ResetFailedAttempts() on successful login
    - _Requirements: 1.5, 1.6, 2.4, 2.7, 2.8_

  - [ ]* 5.2 Write property tests for password validation (Property 1)
    - **Property 1: Password Validation Correctness**
    - Generate random strings: valid (≥12 chars, upper+lower+digit+special) → accept; invalid (missing any rule) → reject
    - **Validates: Requirements 1.2**

  - [ ]* 5.3 Write property tests for role toggle (Properties 2, 3, 4)
    - **Property 2: Role Toggle Produces Opposite Role** — toggling mentor → mentee, toggling twice → original
    - **Property 3: Single Active Role Invariant** — any sequence of operations → exactly one active role
    - **Property 4: Role Toggle Preserves Inactive Profile** — toggle does not modify inactive profile
    - **Validates: Requirements 2.4, 2.7, 2.8**

  - [x] 5.4 Implement authentication handlers (signup, signin, verify, signout, refresh)
    - Implement GoogleOAuthHandler: create Cognito user, issue JWT, redirect to role selection
    - Implement EmailSignupHandler: validate password (12+ chars, upper/lower/digit/special), send verification code (10-min expiry)
    - Implement VerifyEmailHandler: validate code within 10min and 5 attempts, activate account
    - Implement SignInHandler: validate credentials, issue JWT (15-min access, 7-day refresh), redirect to dashboard
    - Implement SignOutHandler: invalidate tokens, clear session
    - Implement RefreshTokenHandler: silent token refresh
    - Implement account lockout: 5 failures in 15min → 30-min lock + email notification
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10_

  - [x] 5.5 Implement role selection and toggle handlers
    - Implement SetRoleHandler: persist initial role to Users_Table, route to onboarding
    - Implement ToggleRoleHandler: switch active role, check if new role needs onboarding, persist to Users_Table
    - Implement role toggle failure rollback (revert on persistence failure)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.9_

  - [x] 5.6 Implement onboarding engine (mentee 4-step + mentor 3-step wizards)
    - Implement SaveOnboardingStepCommand handler with step-level validation via FluentValidation
    - Mentee steps: Profile (name, optional photo, chapter, city), Skills (skills 1-10, level, years), Goals (goal, description 50-500, duration), Preferences (availability, communication, optional resume)
    - Mentor steps: Profile (name, optional photo, chapter, title, company), Expertise (areas 1-10, years 1-30, certs, topics 1-10), Availability (maxMentees 1-5, days/slots, formats, bio 100-1000)
    - Implement progress persistence (save on navigate away, resume from last step)
    - On completion: persist to Mentors/Mentees_Table, set onboardingStatus=completed, redirect to dashboard
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [ ]* 5.7 Write property tests for onboarding validation (Property 5)
    - **Property 5: Onboarding Validation Accepts Valid Data and Rejects Invalid Data**
    - Generate random mentee/mentor profiles: valid (all constraints met) → accept; any field violation → reject with field-level errors
    - **Validates: Requirements 3.2, 3.3, 3.4, 3.5, 3.8, 4.2, 4.3, 4.4, 4.7**

  - [x] 5.8 Implement file upload handlers (presigned URLs for resume + profile photo)
    - Implement GetPresignedUrlHandler: generate S3 upload URL (5-min expiry) for PDF/DOCX (≤5MB) or JPEG/PNG (≤2MB)
    - Implement GetDownloadUrlHandler: generate S3 download URL (15-min expiry) for matched mentors
    - Validate file format and size before generating URL
    - _Requirements: 19.1, 19.2, 19.4, 19.5, 19.7_

  - [x] 5.9 Implement Super Admin domain model and command handlers
    - Create AdminUser entity with LinkedUserId, AdminEmail, IsMfaEnabled, CreatedAt
    - Implement DisableUserCommand/EnableUserCommand handlers with required reason field, audit log recording
    - Implement SetMaintenanceModeCommand handler: store flag in AppConfig, record audit entry
    - Implement ToggleFeatureFlagCommand handler: update AppConfig feature flag, record audit entry
    - Enforce max 5 Super_Admin accounts (reject 6th creation attempt)
    - _Requirements: 31.1, 31.4, 31.5, 31.6, 31.9, 31.10_

  - [x] 5.10 Implement Super Admin API endpoints and middleware
    - Implement GetAdminDashboardHandler: total user counts, active sessions, health status from CloudWatch alarms, recent audit log
    - Implement SearchUsersHandler: search/filter by name, email, role, chapter, onboarding status, account status with pagination
    - Implement GetAuditLogHandler: paginated audit log with filters (date range, action type, admin)
    - Implement maintenance mode middleware: check AppConfig flag on every request; if enabled + non-admin → 503
    - Implement admin JWT authorizer (separate from standard user authorizer, requires admin Cognito group + MFA)
    - _Requirements: 31.2, 31.3, 31.5, 31.7, 31.8_

  - [ ]* 5.11 Write property test for maintenance mode blocking (Property 33)
    - **Property 33: Super Admin Maintenance Mode Blocks All Non-Admin Requests**
    - For any non-admin request during maintenance → 503; for any admin request → proceeds normally
    - **Validates: Requirements 31.5, 31.7**

  - [x] 5.12 Implement Identity Context frontend (login, signup, onboarding, settings pages)
    - Create LoginPage with Google OAuth button + email/password form, generic error messages
    - Create SignupPage with password validation (inline, 300ms debounce), email verification step
    - Create OnboardingWizard: 4-step mentee / 3-step mentor with visual progress indicator, step persistence, inline validation, aria-live error announcements
    - Create SettingsPage: editable profile fields per active role, read-only inactive role section, photo upload, resume upload
    - Implement role selection screen (blocks navigation until selected)
    - _Requirements: 1.1, 1.2, 1.5, 1.8, 2.1, 2.3, 3.1, 3.8, 4.1, 4.7, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.8_

  - [x] 5.13 Implement Super Admin dashboard frontend page
    - Create AdminDashboardPage: user counts by role, active sessions, platform health (alarm states), recent audit log entries
    - Create user search/filter panel (name, email, role, chapter, status)
    - Create user management actions (disable/enable with reason modal + confirmation dialog)
    - Create maintenance mode toggle with estimated return time input
    - Create feature flag toggle panel (AI Help, Job Board, Meetup Calendar, Session Plans)
    - Create audit log viewer with date/action filters
    - Protect admin routes with admin role guard
    - _Requirements: 31.2, 31.3, 31.4, 31.5, 31.6, 31.8_

  - [x] 5.14 Implement host shell NavBar with role toggle and notification bell
    - Create NavBar component with navigation links adapting to active role
    - Create RoleToggle one-click button in NavBar (calls ToggleRoleHandler)
    - Create NotificationBell with unread count badge (1-99 exact, 99+ overflow)
    - Implement AuthProvider (JWT state, silent refresh) and RoleProvider (active role context)
    - _Requirements: 2.4, 2.7, 12.5, 18.2_

  - [ ]* 5.15 Write property test for notification badge display (Property 16)
    - **Property 16: Notification Badge Display**
    - For any N: 1≤N≤99 → display N; N>99 → display "99+"; N=0 → hidden
    - **Validates: Requirements 12.5**

- [x] 6. Checkpoint — Identity context complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Phase 4 — Mentoring Context (Matching, Locking, Sessions, Jobs, Availability)

  - [x] 7.1 Implement matching algorithm domain service (CompatibilityScore computation)
    - Implement MatchingEngine.Compute() as pure function returning CompatibilityScore value object
    - Implement ComputeChapterScore: +30 same chapter, +15 same city different chapter, +0 otherwise
    - Implement ComputeSkillsScore: round((overlap / menteeSkillsCount) × 30), 0 if no skills
    - Implement ComputeGoalScore: round((matchingTopics / relatedTopics) × 25), 0 if no goal
    - Implement ComputeExperienceScore: ≥2 gap → 15, 1 → 10, 0 → 5, negative → 0
    - Implement GetBrowseResults: sort descending by score, then alpha by name, paginate (12/page)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10_

  - [ ]* 7.2 Write property tests for matching algorithm (Properties 6, 7, 8, 9, 10)
    - **Property 6: Matching Algorithm Score Bounds and Determinism** — score ∈ [0,100], deterministic
    - **Property 7: Matching Dimension Formulas** — skills round((M/N)×30), goal round((match/total)×25), 0 for empty
    - **Property 8: Browse Results Sorted Correctly** — descending score, alpha for ties
    - **Property 9: Browse Results Exclude Full-Capacity Mentors** — no mentor where activeMenteeCount ≥ maxMentees
    - **Property 10: Pagination Returns At Most PageSize Items** — |items| ≤ pageSize, total across pages = available mentors
    - **Validates: Requirements 5.1-5.10**

  - [x] 7.3 Implement mentor availability toggle (domain model extension + command handler)
    - Add MentorAvailability value object (Status, Reason, ReturnDate, UnavailableSince) to MentorProfile
    - Add availabilityStatus, unavailabilityReason, returnDate, unavailableSince attributes to Mentors_Table
    - Implement SetMentorAvailabilityCommand handler: update status, set UnavailableSince on unavailable
    - Update BrowseMentorsQuery to exclude mentors where availabilityStatus = 'unavailable'
    - Implement daily EventBridge job: check mentors unavailable >90 days → send reminder notification
    - _Requirements: 32.1, 32.2, 32.3, 32.4, 32.5, 32.6, 32.7, 32.8_

  - [ ]* 7.4 Write property test for mentor availability exclusion from browse (Property 34)
    - **Property 34: Mentor Availability Toggle Excludes from Browse**
    - For any mentor with availabilityStatus='unavailable' → never in browse results; availabilityStatus='available' AND activeMenteeCount < maxMentees → appears
    - **Validates: Requirements 32.2, 32.5**

  - [x] 7.5 Implement locking mechanism (DynamoDB conditional writes + TTL)
    - Implement AcquireLockCommand: conditional write (attribute_not_exists OR expired), 15-min TTL, one lock per mentee
    - Implement ReleaseLockCommand: delete lock record, make mentor available
    - Implement ConfirmSelectionCommand: create pending session record, notify mentor
    - Implement lock TTL expiration cleanup (EventBridge scheduler every 5 min)
    - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ]* 7.6 Write property test for one active lock per mentee (Property 11)
    - **Property 11: One Active Lock Per Mentee**
    - For any mentee with active lock → second lock always rejected; at most one active lock at any time
    - **Validates: Requirements 6.3**

  - [x] 7.7 Implement session management and completion flow handlers
    - Implement AcceptRequestCommand: validate mentor below capacity, update session status to active, trigger plan generation event
    - Implement DeclineRequestCommand: notify mentee, release slot, remove from dashboard
    - Implement MarkCompleteCommand with state machine: mentee marks first → mentor confirms → status=completed; reject mentor-first; reject mentee retraction
    - Implement capacity update on completion: decrement activeMenteeCount
    - Implement 7-day reminder (EventBridge) and 14-day escalation to unresolved
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 11.3, 11.4, 11.5_

  - [ ]* 7.8 Write property tests for completion flow (Properties 14, 15)
    - **Property 14: Completion Flow State Machine Ordering** — mentor can only complete after mentee; valid sequence → status=completed
    - **Property 15: Mentee Completion Is Irrevocable** — once menteeCompletedAt is set, never reverts to null
    - **Validates: Requirements 9.2, 9.5, 9.6**

  - [x] 7.9 Implement Opportunities Board domain model and CRUD handlers
    - Create OpportunityPosting entity (Title, Type[job/workshop/event/training], OrganisationName, Description, Location, EventDateTime, EmploymentType, RequiredSkills, RequiredExperience, ExternalUrl, PublishedAt, ExpiresAt, IsExpired, IsActive)
    - Implement CreateOpportunityCommand: validate max 5 active per mentor (all types combined), compute expiry (30 days or event date, whichever first)
    - Implement RenewOpportunityCommand: extend expiry by 30 days (jobs only)
    - Implement ArchiveOpportunityCommand: set status=archived
    - Implement BrowseOpportunitiesQuery: filters (type, location, skills, experience), sort by publishedAt desc, paginate
    - Implement GetMentorOpportunitiesQuery: mentor's own postings (all types)
    - Implement opportunity expiry job: EventBridge → archive expired/past-event, notify mentor with renewal option (jobs only)
    - Implement OpportunityPublishedEvent handler: notify matched mentees + skill-matched opt-in mentees (≥2 skill overlap)
    - Implement mentee opportunity notification preferences (type filter + skill-match toggle)
    - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.6, 28.9, 28.10, 28.11, 28.12, 28.13_

  - [ ]* 7.10 Write property tests for opportunities board (Properties 21, 22, 23)
    - **Property 21: Opportunity Posting Maximum Active Limit Invariant** — never >5 active (all types combined); 6th rejected
    - **Property 22: Opportunity Posting Expiry Computation** — jobs: PublishedAt + 30 days; events: min(PublishedAt+30, EventDateTime); expired excluded from active results
    - **Property 23: Opportunity Filter Correctness** — all returned results satisfy every filter (type, location, skills, level), sorted publishedAt desc
    - **Validates: Requirements 28.2, 28.3, 28.4, 28.6, 28.10**

  - [ ]* 7.11 Write property test for opportunity badge visibility (Property 24)
    - **Property 24: Opportunity Badge and Mentor Relationship Visibility**
    - Mentor with active postings (any type) → "Sharing Opportunities" badge; mentee with session from mentor with postings → "From your mentor" badge
    - **Validates: Requirements 28.5, 28.8**

  - [x] 7.12 Implement Mentoring Context frontend (Browse, Locking, Sessions, Opportunities, Availability)
    - Create BrowsePage: paginated mentor cards (12/page) with CompatibilityBadge, expertise tags, availability summary, "Sharing Opportunities" badge, keyboard navigation between cards
    - Create MentorCard component with score badge (colour-coded: green >80%, orange 50-79%, red <50%), locked overlay for unavailable mentors
    - Create LockConfirmModal: 15-min timer display, confirm/cancel buttons
    - Create FilterPanel: chapter, skills, availability filters
    - Create SessionListPage: active/completed/pending sessions
    - Create OpportunitiesPage: browse all active postings with filters (type[job/workshop/event/training], location, skills, experience), contextual action button ("Apply" for jobs, "Register" for events/workshops/training)
    - Create OpportunityPostingCard (title, type badge, organisation, location, date for events, skills tags, days remaining)
    - Create OpportunityPostingForm (create/edit) for mentors: type selector, organisation (free text), description, date picker (events only), employment type (jobs only)
    - Create "Sharing Opportunities" badge component for mentor cards
    - Implement mentee opportunity notification preferences in Settings (type filter checkboxes + skill-match toggle)
    - Implement availability toggle on Mentor Dashboard and Settings page (one-click Available/Unavailable with reason + return date)
    - Display "On Break" badge with return date for matched mentees viewing unavailable mentor
    - _Requirements: 5.9, 6.1, 6.7, 6.8, 28.1, 28.5, 28.6, 28.7, 28.8, 28.13, 32.1, 32.3, 32.6, 32.8, 32.9_

  - [x] 7.13 Implement settings update handler (mentor-specific: maxMentees constraint)
    - UpdateSettingsHandler: validate all inputs same as onboarding rules
    - Enforce mentors can only update maxMentees to value ≥ current activeMenteeCount
    - Recalculate compatibility scores on chapter change (flag for next browse)
    - _Requirements: 13.2, 13.6, 13.7_

- [x] 8. Checkpoint — Mentoring context complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Phase 5 — Content Context (AI Session Plan Generation)

  - [x] 9.1 Implement SessionPlan domain model and validation
    - Create SessionPlan record: SessionTitle (≤100 chars), Agenda (3-7 items, each ≥3 min, sum=35), PreworkTasks (2-5, each ≤200 chars), FollowUpTasks (2-5, each ≤200 chars)
    - Create AgendaItem record (Title, DurationMinutes, Description ≤500 chars)
    - Implement IsValid() method: count checks, duration sum check, min per-item check
    - _Requirements: 7.2, 7.3_

  - [ ]* 9.2 Write property test for session plan structural validity (Property 12)
    - **Property 12: Session Plan Structural Validity**
    - For any valid SessionPlan: 3-7 items, each ≥3 min, sum=35, prework 2-5 items (≤200), followup 2-5 (≤200), title ≤100
    - **Validates: Requirements 7.2, 7.3**

  - [x] 9.3 Implement input sanitization and prompt injection prevention
    - Create InputSanitizer: strip control characters, escape prompt override patterns, enforce max 2000 chars/field
    - Neutralize injection patterns (ignore previous, system:, you are now, forget everything) by wrapping in [filtered: ...]
    - Escape template delimiters (``` → ''', --- → —)
    - _Requirements: 7.10, 14.9_

  - [ ]* 9.4 Write property test for input sanitization (Property 18)
    - **Property 18: Input Sanitization Prevents Prompt Injection**
    - For any string with injection patterns → neutralized; non-malicious content preserved; no unmodified injection passes through
    - **Validates: Requirements 7.10, 14.9**

  - [x] 9.5 Implement Semantic Kernel SessionPlanPlugin with Bedrock Converse API
    - Create SessionPlanPlugin with GeneratePlanAsync method using IChatClient abstraction
    - Build prompt template with typed input variables (mentee profile, mentor profile, goals)
    - Configure structured JSON output parsing for SessionPlan schema
    - Implement response validation: schema check + agenda sum = 35 min
    - On invalid response: discard and retry (count toward max 3)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 17.1, 17.2, 17.3, 17.5_

  - [x] 9.6 Implement Bedrock Guardrails configuration
    - Configure content filters (hate, insults, sexual, violence → High threshold)
    - Configure denied topics (financial advice, medical, political, religious, non-AWS)
    - Configure PII redaction (email, phone, address, SSN, credit card) on outputs
    - Implement output validation: check no PII present, no harmful content, schema conformance before persist
    - _Requirements: 7.11, 7.12, 21.17_

  - [x] 9.7 Implement GenerateSessionPlanCommand handler with retry and circuit breaker
    - Implement command handler: invoke Semantic Kernel plugin, validate response, persist to Sessions_Table
    - Implement Polly v8 retry (3x: 2s, 4s, 8s exponential backoff) with circuit breaker (5 failures/30s → 60s break)
    - On all retries exhausted: set session status=pending_plan, publish PlanGenerationFailed to EventBridge (5-min delay)
    - Log Bedrock token usage (input/output tokens) as custom CloudWatch metrics
    - Notify both parties on success or graceful degradation message on failure
    - _Requirements: 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 24.5_

  - [x] 9.8 Implement Content Context frontend (Session Plan Page with streaming)
    - Create SessionPlanPage: display session title, timed agenda (card per item with duration label), prework checklist, followup checklist
    - Implement useObject() streaming via Vercel AI SDK 6 for real-time plan display during generation
    - Implement checklist toggle with optimistic UI (immediate update, revert on failure, retry button)
    - Implement ProgressBar: round((checked/total) × 100)%
    - Display loading skeleton for pending-plan state
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

  - [ ]* 9.9 Write property test for checklist progress calculation (Property 13)
    - **Property 13: Checklist Progress Calculation**
    - For any boolean arrays (prework + followup): progress = round((total_checked / total_items) × 100); 0 when total_items=0
    - **Validates: Requirements 8.5**

- [x] 10. Checkpoint — Content context complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Phase 6 — Engagement Context (Notifications, AI Help, Tour, Dashboards, Meetups, Analytics)

  - [x] 11.1 Implement Notification domain model and handlers
    - Create Notification entity (RecipientUserId, Type, Message ≤500, ActionUrl, IsRead, CreatedAt)
    - Implement CreateNotificationCommand: persist to Notifications_Table, push via AppSync subscription
    - Implement MarkNotificationReadCommand + BatchMarkReadCommand
    - Implement GetNotificationsQuery: return last 50, reverse chronological, unread distinguished
    - Implement GetUnreadCountQuery: count for badge display
    - Implement AppSync subscription for real-time delivery (< 5 seconds)
    - Implement exponential backoff reconnection on WebSocket disconnect
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7_

  - [ ]* 11.2 Write property test for notification ordering (Property 17)
    - **Property 17: Notifications Ordered Reverse Chronologically**
    - For any notification list: each item's createdAt ≥ next item's createdAt; list ≤ 50 items
    - **Validates: Requirements 12.3**

  - [x] 11.3 Implement AI Help Assistant backend (Semantic Kernel plugin + streaming)
    - Create HelpAssistantPlugin with system prompt containing platform documentation
    - Implement ChatWithAssistantCommand: sanitize input (max 1000 chars, strip injection), invoke IChatClient, stream response
    - Constrain responses to platform-related topics, redirect off-topic politely
    - Configure Bedrock Guardrails: prevent system prompt disclosure, harmful content, unrelated topics
    - Implement rate limiting: 20 messages/minute/user
    - _Requirements: 14.2, 14.3, 14.4, 14.5, 14.6, 14.9, 14.10, 14.11, 17.4_

  - [x] 11.4 Implement Meetup Calendar domain model and handlers
    - Create MeetupEvent entity (Chapter, Title, EventDate, StartTime, EndTime, VenueName, VenueAddress, EventUrl, CreatedBy, IsCancelled, ConfirmedAttendees)
    - Implement CreateMeetupEventCommand: validate chapter_lead flag, persist to Meetups_Table
    - Implement CancelMeetupEventCommand: validate chapter_lead, identify aligned sessions, notify affected pairs
    - Implement ConfirmMeetupAttendanceCommand / WithdrawAttendanceCommand for mentors
    - Implement AlignSessionToMeetupCommand: associate session with meetup, adapt agenda context
    - Implement GetUpcomingMeetupsQuery: filter by chapter, max 3, sorted by eventDate asc, exclude cancelled/past
    - Implement 24-hour reminder notification for meetup-aligned sessions
    - _Requirements: 29.1, 29.2, 29.3, 29.4, 29.5, 29.6, 29.7, 29.8, 29.9_

  - [ ]* 11.5 Write property tests for meetup domain (Properties 25, 26, 27, 28)
    - **Property 25: Meetup-Session Alignment Preserves Meetup Data** — session references meetup date, time, venue correctly
    - **Property 26: Chapter Lead Authorization for Meetup Management** — non-lead → rejected; lead → succeeds
    - **Property 27: Meetup Cancellation Identifies All Affected Sessions** — exactly N sessions notified, no unrelated
    - **Property 28: Upcoming Meetups Query Returns Chapter-Filtered Sorted Results** — max 3, correct chapter, sorted asc, no cancelled/past
    - **Validates: Requirements 29.3, 29.6, 29.7, 29.8, 29.9**

  - [x] 11.6 Implement Engagement Analytics tracking (frontend EventTracker + backend ingest)
    - Create EventTracker class: buffer events, flush every 30s, sendBeacon on visibilitychange
    - Create TrackerProvider context and useTracker hook (trackPageView, trackClick, trackFormStep, trackError, trackA11y)
    - Implement IngestEventsHandler: batch persist to EngagementEvents_Table with hashed userId (SHA-256)
    - Tag all events with activeRole (mentor/mentee)
    - Implement consent banner (opt-in/out); opt-out disables non-essential events, keeps auth/error only
    - Implement UpdateConsentHandler to persist user consent preference
    - _Requirements: 30.1, 30.2, 30.3, 30.7, 30.8, 30.11_

  - [ ]* 11.7 Write property tests for engagement analytics (Properties 29, 30, 31, 32)
    - **Property 29: Tracked Events Contain No PII** — no raw email/phone/address/name; userId is SHA-256 hash
    - **Property 30: Event Schema Completeness With Role Tagging** — all required fields present; activeRole is mentor or mentee
    - **Property 31: Consent Opt-Out Disables Non-Essential Tracking** — opted-out → zero non-essential events
    - **Property 32: Event Buffer Flush and Retry Integrity** — after successful flush buffer empty; after failure all events re-added in order
    - **Validates: Requirements 30.1, 30.2, 30.3, 30.7, 30.8, 30.11**

  - [x] 11.8 Implement operator analytics dashboard backend
    - Implement GetAnalyticsDashboardHandler: DAU/WAU/MAU, feature heatmap, error hotspots, retention (admin only)
    - Implement GetFunnelDataHandler: signup→onboard→browse→match→session→complete funnel (admin only)
    - Implement DynamoDB Streams → Aurora replication Lambda for complex analytical queries
    - Implement engagement-specific analytics: browse-to-lock conversion, plan-to-completion rate, job view-to-click rate
    - _Requirements: 30.4, 30.5, 30.6, 30.9, 30.10_

  - [x] 11.9 Implement Dashboard handlers (Mentee + Mentor)
    - Implement GetMenteeDashboardQuery: active session cards (mentor name, title, next followup, progress %), top 3 recommended mentors, stats (completed, total checklist items, %)
    - Implement GetMentorDashboardQuery: pending requests (oldest first, with compatibility scores), active mentee cards, capacity indicator (active/max), availability status toggle
    - Implement empty states with call-to-action guidance
    - Implement error recovery: per-section error with retry, preserve loaded sections
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.6, 11.7_

  - [x] 11.10 Implement Engagement Context frontend (Dashboards, Notifications, AI Help, Tour, Meetups, Analytics)
    - Create MenteeDashboard: active session cards, top 3 mentors, progress stats, summary bar, upcoming meetups (3), empty states
    - Create MentorDashboard: pending requests (accept/decline), active mentees, capacity indicator, availability toggle, upcoming meetups (3)
    - Create NotificationPanel: last 50 notifications, unread indicator, click-to-navigate, batch mark-all-read
    - Create AIHelpAssistant: floating bubble (bottom-right), useChat() streaming, Ctrl+H shortcut, dismissible, session-scoped history, ARIA labels
    - Create OnboardingTour: step-by-step overlay with tooltips, keyboard nav (Tab advance, Escape dismiss), aria-live announcements, dismissible, restart from settings
    - Create MeetupCalendar: upcoming meetups display, MeetupAlignModal for session scheduling, MeetupBadge ("Attending [meetup]")
    - Create ConsentBanner: first-visit tracking consent with opt-out
    - Create operator analytics dashboard frontend (admin-only route): DAU/WAU/MAU charts, feature heatmap, funnels, error hotspots
    - _Requirements: 10.1, 10.3, 11.1, 11.2, 12.3, 12.5, 14.1, 14.2, 14.7, 14.8, 25.1, 25.2, 25.3, 29.9, 30.6, 30.7_

- [x] 12. Checkpoint — Engagement context complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Phase 7 — Cross-Cutting Concerns (Security, CI/CD, Observability, HA/DR, Architecture Tests)

  - [x] 13.1 Implement security middleware and headers
    - Implement JWT validation middleware (Cognito authorizer, 15-min access token)
    - Implement resource-based access control (userId in JWT must match resource owner, else 403)
    - Implement rate limiting middleware (100 req/min/user, 429 + Retry-After)
    - Implement request body size limit (1 MB max)
    - Configure security response headers: CSP, X-Content-Type-Options: nosniff, X-Frame-Options: DENY, HSTS (max-age 31536000)
    - Implement CSRF protection (SameSite=Strict + Origin validation)
    - _Requirements: 15.2, 15.3, 15.8, 15.9, 21.1, 21.2, 21.3, 21.4, 21.5, 21.6_

  - [x] 13.2 Implement audit logging for all state changes
    - Create audit log writer: record userId, timestamp, action, resource, correlationId to dedicated CloudWatch log group
    - Instrument all command handlers with audit logging
    - Implement admin audit logging (adminId, action, target, reason) for super admin operations
    - _Requirements: 21.9, 21.17, 31.9_

  - [x] 13.3 Implement health check endpoints and feature flag integration
    - Add /v1/health endpoint per microservice (returns 200 when service + dependencies operational)
    - Integrate AWS AppConfig feature flags (AI Help, Job Board, Meetup Calendar, Session Plans)
    - Implement canary deployment support (1% → 10% → 50% → 100%)
    - _Requirements: 22.6, 23.6_

  - [~] 13.4 Implement GitHub Actions CI/CD workflows
    - Create ci-dotnet.yml: build, xUnit tests, FsCheck property tests, coverage (block <80% handlers, <95% pure logic)
    - Create ci-react.yml: build, Vitest tests, axe-core checks (block <90)
    - Create deploy-infrastructure.yml: Terraform plan (PR) / apply (merge)
    - Create deploy-backend.yml: Native AOT publish → zip → Lambda deploy
    - Create deploy-frontend.yml: Vite build → S3 upload → CloudFront invalidation
    - Create e2e-tests.yml: Playwright against staging
    - Create security-scan.yml: OWASP ZAP + NuGet Audit + npm audit
    - _Requirements: 23.2, 23.3, 23.4, 23.5, 23.7_

  - [~] 13.5 Implement background jobs and event-driven handlers
    - Implement DynamoDB Streams → Aurora replication Lambda (analytics data sync)
    - Implement 7-day completion reminder event handler
    - Implement 14-day escalation event handler (set unresolved, notify both)
    - Implement lock expiration cleanup (every 5 min)
    - Implement job posting expiry handler (daily: archive expired, notify mentor)
    - Implement analytics aggregation (hourly)
    - Implement notification digest (daily 9AM AEST)
    - Implement mentor 90-day unavailability reminder (daily)
    - _Requirements: 20.2, 20.3, 20.4, 20.5, 20.6, 20.7, 32.7_

  - [~] 13.6 Implement DynamoDB partition key strategies and performance optimizations
    - Implement Notifications_Table composite key pattern (recipientUserId#YYYY-MM) for write distribution
    - Configure auto-scaling alarms at 70% consumed capacity
    - Implement connection pooling via RDS Proxy for Aurora queries (staging/prod only)
    - _Requirements: 26.1, 26.2, 26.3, 26.4_

  - [ ]* 13.7 Write architecture tests (NetArchTest) for SOLID enforcement
    - Verify Domain layer: no dependency on Infrastructure/Application/Presentation, no AWS SDK refs
    - Verify Application layer: no dependency on Infrastructure/Presentation
    - Verify SOLID: all Domain classes implement at most 1 interface (ISP), all repo interfaces in Domain (DIP), all handlers have single Handle method (SRP)
    - _Requirements: 27.1, 27.2, 27.5_

  - [~] 13.8 Implement guided help flow and UX polish
    - Implement inline real-time validation (300ms debounce) on all form fields with tooltips explaining constraints
    - Implement confirmation dialogs for destructive actions (decline mentee, cancel session, delete profile)
    - Implement empty state guidance on all pages (clear instructions + CTA buttons)
    - Implement progress indicators on all multi-step flows
    - Implement friendly non-technical error messages with retry buttons
    - Implement retry mechanism with visual spinner on all failure states
    - _Requirements: 25.4, 25.5, 25.6, 25.7, 25.8, 25.9, 25.10_

  - [~] 13.9 Implement ISO/Essential Eight compliance artifacts
    - Document information classification (public, internal, confidential, restricted) for all data stores
    - Document access control register (who can access what data)
    - Implement data retention policies (user data 3 years after last activity, delete on request within 30 days)
    - Document AI risk assessment for session plan generation and help assistant
    - Implement model version tracking (record Bedrock model version per session plan)
    - Implement human oversight: admin can review/flag AI-generated session plans
    - Configure Lambda code signing (staging/prod) and container image scanning
    - _Requirements: 21.15, 21.16, 21.17_

- [~] 14. Checkpoint — Cross-cutting concerns complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 15. Phase 8 — Seed Data, E2E Testing, and Deployment

  - [~] 15.1 Implement Seed Data Generator CLI tool (tools/SeedData project)
    - Create .NET console project at tools/SeedData with DynamoDB client
    - Implement SeedDataGenerator orchestrator: idempotent (check seed marker before creating)
    - Reject production execution (throw if environment=prod)
    - Accept environment via CLI argument: `dotnet run --project tools/SeedData -- --environment dev`
    - _Requirements: 33.1, 33.8, 33.9_

  - [~] 15.2 Implement Bogus generators for Australian-specific seed data
    - Create AustralianFakers: MentorFaker (20 profiles across 8+ chapters, varied expertise/certs/experience)
    - Create MenteeFaker (30 profiles, varied skills/goals/chapters)
    - Create JobPostingFaker (10 active, 3 expired, 2 archived from different mentors)
    - Create MeetupEventFaker (5 upcoming across 5 chapters with attendees, 2 past)
    - Create NotificationFaker (50 records, all types, varied read/unread)
    - Use Bogus "en_AU" locale for realistic Australian names, emails, company names
    - _Requirements: 33.2, 33.3, 33.4, 33.5, 33.7_

  - [~] 15.3 Implement seed data orchestration (all entity creation with relationships)
    - Seed Super Admin account (admin@guidedmentor.dev, MFA enabled)
    - Seed 2 chapter lead accounts (Sydney + Melbourne, chapter_lead flag)
    - Seed 20 mentors (including 1 with availabilityStatus=unavailable and returnDate set)
    - Seed 30 mentees
    - Seed sessions: 15 active (various checklist 10-90%), 5 completed, 3 pending, 2 unresolved
    - Seed 3 sessions aligned to meetup events
    - Seed 1 dual-role user (both onboardings complete, for role toggle demo)
    - Set seed marker after successful completion
    - _Requirements: 33.2, 33.3, 33.4, 33.5, 33.6, 33.8_

  - [ ]* 15.4 Write property test for seed data idempotency (Property 35)
    - **Property 35: Seed Data Generator Is Idempotent**
    - For any N≥1 executions → dataset identical to single execution; no duplicates, constant record count
    - **Validates: Requirements 33.8**

  - [~] 15.5 Create demo guide document (docs/demo-guide.md)
    - Document step-by-step presenter walkthrough of all features using seed accounts
    - Include login credentials for: Super Admin, chapter lead, mentor, mentee, dual-role user
    - Document expected outcomes per step (screenshots optional, descriptions required)
    - Cover all user journeys: auth → onboard → browse → lock → session plan → complete → role toggle → job board → meetup → admin dashboard
    - _Requirements: 33.10_

  - [ ]* 15.6 Write Playwright E2E tests for critical user journeys
    - Journey 1: Sign up (Google OAuth mock) → role selection → mentee onboarding → dashboard
    - Journey 2: Browse mentors → lock → confirm → session created → mentor accepts
    - Journey 3: Session plan generated → view plan → toggle checklist items → progress updates
    - Journey 4: Mark complete (mentee) → confirm (mentor) → session completed
    - Journey 5: Role toggle → switch dashboard → toggle back (preserves both profiles)
    - Journey 6: Job board: create posting → browse opportunities → apply click
    - Journey 7: Meetup calendar: create event (chapter lead) → align session → reminder
    - Journey 8: Super Admin: login → user management → maintenance mode → feature toggle
    - Include axe-core accessibility checks on every page navigation
    - _Requirements: 23.4, 27.4_

  - [ ]* 15.7 Write accessibility E2E tests
    - Verify skip-navigation link present and functional on all pages
    - Verify keyboard navigation through all interactive elements (Tab, Enter, Escape)
    - Verify aria-live announcements for dynamic content (errors, notifications, loading)
    - Verify focus management in modals, wizards, and dropdowns (focus trap)
    - Verify colour contrast (4.5:1 normal text, 3:1 large text) via axe-core
    - Verify 200% zoom without overflow or functionality loss
    - _Requirements: 18.5, 18.6, 18.7, 18.8, 25.1, 25.3_

  - [~] 15.8 Final integration wiring and deployment verification
    - Verify all Module Federation remotes load correctly in host shell
    - Verify API Gateway routes map to correct Lambda handlers across all 4 contexts
    - Verify AppSync subscriptions deliver real-time notifications end-to-end
    - Verify DynamoDB Streams → Aurora replication pipeline
    - Verify EventBridge scheduler triggers fire correctly
    - Verify CloudFront serves SPA assets with correct cache headers
    - Deploy to dev environment and run seed data generator
    - _Requirements: 15.1, 18.2, 23.1_

- [~] 16. Final checkpoint — All tests pass, platform deployable
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation between phases
- Property tests (FsCheck) validate all 35 correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Environment rules: Dev/demo skips Aurora, dormant WAF/Guardrails/CMK/RDS Proxy; all 8 DynamoDB tables deployed (free tier). Staging/prod: everything enabled
- Super Admin features are in the Identity Context (admin endpoints, maintenance mode middleware)
- Mentor Availability Toggle is in the Mentoring Context (availability status field, browse exclusion logic)
- Seed Data Generator is a standalone tools project (tools/SeedData) — Phase 8 before E2E tests
- Profile photo is OPTIONAL in onboarding (can be skipped and uploaded later from Settings)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "1.4", "1.5", "1.6"] },
    { "id": 2, "tasks": ["1.7", "1.8", "1.9", "1.10"] },
    { "id": 3, "tasks": ["3.1"] },
    { "id": 4, "tasks": ["3.2", "3.3", "3.4", "3.5"] },
    { "id": 5, "tasks": ["3.6", "3.7", "3.8", "3.9", "3.9.1"] },
    { "id": 6, "tasks": ["5.1", "5.4"] },
    { "id": 7, "tasks": ["5.2", "5.3", "5.5", "5.6"] },
    { "id": 8, "tasks": ["5.7", "5.8", "5.9"] },
    { "id": 9, "tasks": ["5.10", "5.11", "5.12"] },
    { "id": 10, "tasks": ["5.13", "5.14"] },
    { "id": 11, "tasks": ["5.15"] },
    { "id": 12, "tasks": ["7.1", "7.3", "7.5"] },
    { "id": 13, "tasks": ["7.2", "7.4", "7.6", "7.7", "7.9"] },
    { "id": 14, "tasks": ["7.8", "7.10", "7.11", "7.12", "7.13"] },
    { "id": 15, "tasks": ["9.1", "9.3"] },
    { "id": 16, "tasks": ["9.2", "9.4", "9.5", "9.6"] },
    { "id": 17, "tasks": ["9.7", "9.8"] },
    { "id": 18, "tasks": ["9.9"] },
    { "id": 19, "tasks": ["11.1", "11.3", "11.4", "11.6"] },
    { "id": 20, "tasks": ["11.2", "11.5", "11.7", "11.8", "11.9"] },
    { "id": 21, "tasks": ["11.10"] },
    { "id": 22, "tasks": ["13.1", "13.2", "13.3"] },
    { "id": 23, "tasks": ["13.4", "13.5", "13.6", "13.7"] },
    { "id": 24, "tasks": ["13.8", "13.9"] },
    { "id": 25, "tasks": ["15.1", "15.2"] },
    { "id": 26, "tasks": ["15.3", "15.5"] },
    { "id": 27, "tasks": ["15.4", "15.6", "15.7"] },
    { "id": 28, "tasks": ["15.8"] }
  ]
}
```
