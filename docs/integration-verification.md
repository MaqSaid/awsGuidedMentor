# GuidedMentor — Integration Verification Checklist

> Complete verification checklist for validating all integration points across the platform.
> Run after each deployment to ensure end-to-end connectivity.

## Quick Start

```bash
# Automated verification (requires AWS CLI + credentials)
./scripts/verify-deployment.sh dev

# Manual verification steps are documented below for each subsystem
```

---

## 1. Module Federation (Frontend Micro-Frontends)

### Host Shell Configuration

| Remote | Port | Exposed Modules | Status |
|--------|------|-----------------|--------|
| identity | 3001 | LoginPage, SignupPage, OnboardingWizard, SettingsPage | ✓ |
| mentoring | 3002 | BrowsePage, SessionListPage, OpportunitiesPage | ✓ |
| content | 3003 | SessionPlanPage, AgendaTimeline, Checklist, ProgressBar | ✓ |
| engagement | 3004 | MenteeDashboard, MentorDashboard, NotificationPanel, AIHelpAssistant, OnboardingTour, MeetupCalendar, OperatorAnalytics, ConsentBanner, TrackerProvider | ✓ |

### Verification Steps

1. **Local Development:**
   ```bash
   cd frontend && npm run dev
   # All 5 apps start (host:3000, identity:3001, mentoring:3002, content:3003, engagement:3004)
   ```

2. **Remote Entry Files:**
   - Each remote produces `dist/assets/remoteEntry.js` on build
   - Host shell loads remotes via `@originjs/vite-plugin-federation`
   - Shared dependencies (react, react-dom, react-router-dom, @tanstack/react-query) are deduped

3. **Deployed (CloudFront):**
   - Host shell served from S3 bucket root
   - Remotes served from `s3://{bucket}/remotes/{name}/`
   - `VITE_REMOTE_BASE_URL` env var configures deployed remote URLs
   - `remoteEntry.js` has `no-cache` headers; other assets have `immutable` cache

4. **Error Handling:**
   - `ErrorBoundary` wraps all lazy-loaded remote components
   - `Suspense` shows `LoadingFallback` during chunk downloads
   - Network failures show friendly error with retry option

---

## 2. API Gateway → Lambda Handler Route Map

### Identity Context (`/v1/auth/*`, `/v1/admin/*`)

| Method | Path | Lambda Handler | Auth |
|--------|------|----------------|------|
| POST | `/v1/auth/signup` | Identity API | None |
| POST | `/v1/auth/signin` | Identity API | None |
| POST | `/v1/auth/verify` | Identity API | None |
| POST | `/v1/auth/signout` | Identity API | JWT |
| POST | `/v1/auth/refresh` | Identity API | Refresh Token |
| POST | `/v1/auth/google` | Identity API | None |
| POST | `/v1/role/select` | Identity API | JWT |
| POST | `/v1/role/toggle` | Identity API | JWT |
| GET | `/v1/admin/dashboard` | Identity API | Admin JWT |
| GET | `/v1/admin/users` | Identity API | Admin JWT |
| PUT | `/v1/admin/users/{id}/disable` | Identity API | Admin JWT |
| PUT | `/v1/admin/users/{id}/enable` | Identity API | Admin JWT |
| POST | `/v1/admin/maintenance` | Identity API | Admin JWT |
| PUT | `/v1/admin/features/{flag}` | Identity API | Admin JWT |
| GET | `/v1/admin/audit-log` | Identity API | Admin JWT |

### Mentoring Context (`/v1/mentors/*`, `/v1/locks/*`, `/v1/sessions/*`, `/v1/opportunities/*`)

| Method | Path | Lambda Handler | Auth |
|--------|------|----------------|------|
| GET | `/v1/mentors` | Mentoring API | JWT |
| GET | `/v1/mentors/{id}` | Mentoring API | JWT |
| PUT | `/v1/mentors/availability` | Mentoring API | JWT |
| GET | `/v1/mentors/availability` | Mentoring API | JWT |
| PUT | `/v1/mentors/settings` | Mentoring API | JWT |
| POST | `/v1/locks` | Mentoring API | JWT |
| DELETE | `/v1/locks/{id}` | Mentoring API | JWT |
| POST | `/v1/locks/{id}/confirm` | Mentoring API | JWT |
| GET | `/v1/sessions` | Mentoring API | JWT |
| GET | `/v1/sessions/{id}` | Mentoring API | JWT |
| POST | `/v1/sessions/{id}/accept` | Mentoring API | JWT |
| POST | `/v1/sessions/{id}/decline` | Mentoring API | JWT |
| POST | `/v1/sessions/{id}/complete` | Mentoring API | JWT |
| GET | `/v1/opportunities` | Mentoring API | JWT |
| POST | `/v1/opportunities` | Mentoring API | JWT |
| PUT | `/v1/opportunities/{id}` | Mentoring API | JWT |
| DELETE | `/v1/opportunities/{id}` | Mentoring API | JWT |
| POST | `/v1/opportunities/{id}/renew` | Mentoring API | JWT |
| GET | `/v1/opportunities/preferences` | Mentoring API | JWT |
| PUT | `/v1/opportunities/preferences` | Mentoring API | JWT |

### Content Context (`/v1/sessions/{id}/plan/*`)

| Method | Path | Lambda Handler | Auth |
|--------|------|----------------|------|
| GET | `/v1/sessions/{id}/plan` | Content API | JWT |
| POST | `/v1/sessions/{id}/plan/generate` | Content API | JWT |
| PUT | `/v1/sessions/{id}/plan/checklist` | Content API | JWT |
| GET | `/v1/sessions/{id}/plan/stream` | Content API (SSE) | JWT |

### Engagement Context (`/v1/notifications/*`, `/v1/assistant/*`, `/v1/analytics/*`)

| Method | Path | Lambda Handler | Auth |
|--------|------|----------------|------|
| GET | `/v1/notifications` | Engagement API | JWT |
| GET | `/v1/notifications/count` | Engagement API | JWT |
| PUT | `/v1/notifications/{id}/read` | Engagement API | JWT |
| PUT | `/v1/notifications/read-all` | Engagement API | JWT |
| POST | `/v1/assistant/chat` | Engagement API (SSE) | JWT |
| POST | `/v1/analytics/events` | Engagement API | Optional |
| PUT | `/v1/analytics/consent` | Engagement API | JWT |
| GET | `/v1/analytics/dashboard` | Engagement API | Admin JWT |
| GET | `/v1/analytics/funnels` | Engagement API | Admin JWT |
| GET | `/v1/analytics/engagement` | Engagement API | Admin JWT |

### Health Check Endpoints (all contexts)

| Method | Path | Lambda | Auth |
|--------|------|--------|------|
| GET | `/v1/health` | Identity API | None |
| GET | `/v1/health` | Mentoring API | None |
| GET | `/v1/health` | Content API | None |
| GET | `/v1/health` | Engagement API | None |

---

## 3. AppSync Subscriptions (Real-Time Notifications)

### Configuration

- **API Type:** GraphQL with Cognito User Pool authorization
- **WebSocket Endpoint:** `wss://{appsync-id}.appsync-realtime-api.{region}.amazonaws.com/graphql`
- **Auth Mode:** AMAZON_COGNITO_USER_POOLS (JWT token in connection params)

### Subscription Operations

```graphql
subscription OnNotificationCreated($recipientUserId: ID!) {
  onNotificationCreated(recipientUserId: $recipientUserId) {
    id
    type
    message
    actionUrl
    isRead
    createdAt
  }
}
```

### Verification Steps

1. Open two browser tabs (mentor + mentee)
2. Perform an action that triggers a notification (e.g., mentee locks mentor)
3. Verify the notification appears in the recipient's bell within 5 seconds
4. Check browser DevTools WebSocket connection shows `ka` (keep-alive) frames
5. Disconnect WiFi briefly → verify reconnection with exponential backoff

---

## 4. DynamoDB Streams → Aurora Replication Pipeline

### Data Flow (Staging/Prod Only)

```
DynamoDB Table (INSERT/MODIFY/REMOVE)
  → DynamoDB Stream (NEW_AND_OLD_IMAGES)
    → Lambda: ddb-stream-replication
      → Aurora PostgreSQL (analytics schema)
```

### Tables with Streams Enabled

| Source Table | Target Aurora Table | Stream Type |
|------|------|------|
| `{env}-guidedmentor-users` | `analytics.users` | NEW_AND_OLD_IMAGES |
| `{env}-guidedmentor-mentors` | `analytics.mentors` | NEW_AND_OLD_IMAGES |
| `{env}-guidedmentor-mentees` | `analytics.mentees` | NEW_AND_OLD_IMAGES |
| `{env}-guidedmentor-sessions` | `analytics.sessions` | NEW_AND_OLD_IMAGES |

### Verification Steps

1. Confirm DynamoDB Streams are enabled on source tables (staging/prod):
   ```bash
   aws dynamodb describe-table --table-name staging-guidedmentor-sessions \
     --query "Table.StreamSpecification"
   ```
2. Confirm Lambda event source mapping exists:
   ```bash
   aws lambda list-event-source-mappings \
     --function-name staging-guidedmentor-ddb-stream-replication
   ```
3. Insert a test record into DynamoDB → verify it appears in Aurora within 30 seconds
4. Check Lambda CloudWatch logs for successful replication events

### Dev Environment

Aurora and DynamoDB Streams replication are **skipped in dev** (`enable_aurora = false`). Analytics queries fall back to DynamoDB direct queries in dev mode.

---

## 5. EventBridge Scheduler Rules

### Scheduled Jobs

| Schedule | Frequency | Lambda Target | DLQ | Purpose |
|----------|-----------|---------------|-----|---------|
| lock-cleanup | Every 5 min | `{env}-guidedmentor-lock-cleanup` | `{env}-guidedmentor-dlq-lock-cleanup` | Release expired 15-min mentor locks |
| analytics-aggregation | Every 1 hour | `{env}-guidedmentor-analytics-aggregation` | `{env}-guidedmentor-dlq-analytics-aggregation` | Aggregate metrics to Aurora (staging/prod only) |
| notification-digest | Daily 9 AM AEST | `{env}-guidedmentor-notification-digest` | `{env}-guidedmentor-dlq-notification-digest` | Send daily notification email digest |
| availability-reminder | Daily midnight UTC | `{env}-guidedmentor-availability-reminder` | `{env}-guidedmentor-dlq-availability-reminder` | Remind mentors unavailable >90 days |

### Event Bus Rules (Event-Driven)

| Rule | Source | Detail-Type | Target |
|------|--------|-------------|--------|
| session-accepted | `guidedmentor.mentoring` | SessionAccepted | Content Lambda (plan generation) |
| completion-marked | `guidedmentor.mentoring` | CompletionMarked | Engagement Lambda (notifications) |
| plan-generation-failed | `guidedmentor.content` | PlanGenerationFailed | Content Lambda (async retry) |

### Verification Steps

1. Check scheduler state:
   ```bash
   aws scheduler list-schedules --query "Schedules[?starts_with(Name,'dev-guidedmentor')]"
   ```
2. Manually invoke lock cleanup to verify wiring:
   ```bash
   aws lambda invoke --function-name dev-guidedmentor-lock-cleanup /dev/null
   ```
3. Publish a test event to verify bus routing:
   ```bash
   aws events put-events --entries '[{
     "Source": "guidedmentor.mentoring",
     "DetailType": "SessionAccepted",
     "Detail": "{\"sessionId\":\"test-123\"}",
     "EventBusName": "dev-guidedmentor"
   }]'
   ```
4. Check DLQ message count (should be 0 in normal operation):
   ```bash
   aws sqs get-queue-attributes \
     --queue-url $(aws sqs get-queue-url --queue-name dev-guidedmentor-dlq-lock-cleanup --query QueueUrl --output text) \
     --attribute-names ApproximateNumberOfMessages
   ```

---

## 6. Terraform Output Consumption

### Cross-Module Output Dependencies

| Producer Module | Output | Consumer |
|----------------|--------|----------|
| identity | `user_pool_id` | engagement (AppSync auth), networking (API authorizer) |
| identity | `user_pool_arn` | networking (Cognito authorizer) |
| security | `waf_web_acl_arn` | networking (CloudFront + API Gateway WAF association) |
| networking | `api_gateway_id` | CI/CD (endpoint URL construction) |
| networking | `api_gateway_execution_arn` | Lambda permission grants |
| networking | `cloudfront_distribution_id` | CI/CD (cache invalidation) |
| networking | `spa_bucket_name` | CI/CD (S3 sync target) |
| events | `event_bus_name` | Application code (EventBridge publish) |
| events | `scheduler_role_arn` | Scheduler targets (Lambda invoke) |

### Environment Variables Consumed by Lambdas

| Variable | Source | Used By |
|----------|--------|---------|
| `USERS_TABLE` | Terraform output | Identity, Engagement |
| `MENTORS_TABLE` | Terraform output | Mentoring, Identity |
| `MENTEES_TABLE` | Terraform output | Mentoring, Identity |
| `SESSIONS_TABLE` | Terraform output | Mentoring, Content |
| `NOTIFICATIONS_TABLE` | Terraform output | Engagement |
| `EVENT_BUS_NAME` | Terraform output | All contexts |
| `APPSYNC_URL` | Terraform output | Engagement |
| `COGNITO_USER_POOL_ID` | Terraform output | All contexts |
| `BEDROCK_MODEL_ID` | Configuration | Content |
| `ENVIRONMENT` | Terraform variable | All contexts |

### Frontend Environment Variables (Build-Time)

| Variable | Source | Description |
|----------|--------|-------------|
| `VITE_API_URL` | GitHub Actions vars | API Gateway invoke URL |
| `VITE_COGNITO_USER_POOL_ID` | GitHub Actions vars | Cognito pool for auth |
| `VITE_COGNITO_CLIENT_ID` | GitHub Actions vars | Cognito app client ID |
| `VITE_APPSYNC_URL` | GitHub Actions vars | AppSync endpoint for subscriptions |
| `VITE_ENVIRONMENT` | GitHub Actions vars | Environment name |
| `VITE_REMOTE_BASE_URL` | GitHub Actions vars | CloudFront URL for remote entry files |

---

## 7. Deployment Procedure

### Dev Environment Deployment

```bash
# 1. Deploy infrastructure
cd infrastructure
terraform init
terraform plan -var-file=environments/dev.tfvars
terraform apply -var-file=environments/dev.tfvars

# 2. Deploy backend (Native AOT Lambdas)
for context in Identity Mentoring Content Engagement; do
  dotnet publish "src/${context}/GuidedMentor.${context}.Api/" \
    --configuration Release --runtime linux-x64 --self-contained true \
    -p:PublishAot=true --output "./publish/${context,,}"
done

# 3. Deploy frontend (build + S3 sync)
cd frontend && npm ci && npm run build
aws s3 sync host-shell/dist/ s3://${BUCKET}/ --delete
for remote in identity mentoring content engagement; do
  aws s3 sync remotes/${remote}/dist/ s3://${BUCKET}/remotes/${remote}/ --delete
done
aws cloudfront create-invalidation --distribution-id ${CF_ID} --paths "/*"

# 4. Run seed data generator
dotnet run --project tools/SeedData -- --environment dev

# 5. Verify deployment
./scripts/verify-deployment.sh dev
```

### CI/CD Pipeline (Automated)

| Workflow | Trigger | Action |
|----------|---------|--------|
| `ci-dotnet.yml` | PR to main (src/**) | Build + test + coverage |
| `ci-react.yml` | PR to main (frontend/**) | Build + Vitest + axe-core |
| `deploy-infrastructure.yml` | Merge to main (infrastructure/**) | Terraform plan → apply |
| `deploy-backend.yml` | Merge to main (src/**) | Native AOT publish → Lambda deploy |
| `deploy-frontend.yml` | Merge to main (frontend/**) | Vite build → S3 → CloudFront invalidation |
| `e2e-tests.yml` | After staging deploy | Playwright E2E suite |
| `security-scan.yml` | Weekly + PR | OWASP ZAP + dependency audit |

---

## 8. Environment-Specific Configuration Matrix

| Feature | Dev | Staging | Prod |
|---------|-----|---------|------|
| DynamoDB tables | ✓ (all 8) | ✓ | ✓ |
| Aurora PostgreSQL | ✗ (skipped) | ✓ | ✓ (Multi-AZ) |
| RDS Proxy | ✗ | ✓ | ✓ |
| WAF | ✗ (dormant) | ✓ | ✓ |
| KMS CMK | ✗ (AWS-managed) | ✓ | ✓ |
| Bedrock Guardrails | ✗ (dormant) | ✓ | ✓ |
| CloudWatch Alarms | ✗ | ✓ | ✓ |
| DynamoDB Streams | ✗ | ✓ | ✓ |
| EventBridge Schedulers | ✓ | ✓ | ✓ |
| AppSync | ✓ | ✓ | ✓ |
| CloudFront | ✓ | ✓ | ✓ (custom domain) |
| Lambda code signing | ✗ | ✓ | ✓ |
| S3 cross-region replication | ✗ | ✗ | ✓ |

---

## 9. Troubleshooting Common Integration Issues

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Remote module fails to load | `remoteEntry.js` not at expected path | Check S3 path matches `VITE_REMOTE_BASE_URL` + remote structure |
| CORS errors on API calls | API Gateway CORS config missing origin | Add origin to `allowed_origins` in networking module |
| 401 on API requests | Cognito token expired or invalid | Check JWT expiry (15 min), verify `VITE_COGNITO_CLIENT_ID` matches |
| AppSync subscription disconnects | WebSocket idle timeout | Client should auto-reconnect with exponential backoff |
| EventBridge target not invoking | Lambda permission missing | Check IAM role `{env}-guidedmentor-scheduler-execution` has `lambda:InvokeFunction` |
| DDB Stream → Aurora lag | Lambda throttling or connection pool exhaustion | Check Lambda concurrent executions and RDS Proxy connections |
| CloudFront serving stale content | Missing cache invalidation | Run `aws cloudfront create-invalidation --paths "/*"` |
| Terraform state lock | Previous apply interrupted | Run `terraform force-unlock {lock-id}` |

---

## 10. Post-Deployment Smoke Test

After successful deployment and seed data generation, manually verify:

- [ ] Login page loads at CloudFront URL
- [ ] Google OAuth redirects correctly
- [ ] Email/password login works with seeded credentials
- [ ] Role toggle switches dashboard
- [ ] Browse mentors shows paginated results with scores
- [ ] Lock a mentor → 15-min timer appears
- [ ] Notifications appear in real-time (< 5s)
- [ ] AI Help assistant responds to questions
- [ ] Session plan page displays agenda + checklists
- [ ] Opportunities board shows active postings
- [ ] Admin dashboard loads with seed data metrics
- [ ] Health endpoints return 200 for all services
