# Access Control Register

> GuidedMentor Platform — ISO/IEC 27001 A.9.1 Compliance
>
> Last reviewed: 2025-01-15 | Next review: 2025-07-15

## Access Control Model

GuidedMentor implements **resource-based access control (RBAC)** enforced at the API Gateway level via Cognito JWT authorisation, with additional resource-ownership checks in Lambda handlers.

### Principles

- **Least privilege**: Each Lambda role has only the permissions it needs
- **Permission boundaries**: All Lambda execution roles bound by `{env}-guidedmentor-lambda-boundary`
- **No IAM users**: Console access via AWS SSO only (AWS Organizations)
- **MFA enforced**: All platform users via Cognito; all AWS accounts via SSO policies
- **Zero-trust IAM**: Explicit deny on cross-account actions, no permanent admin access

## Role Definitions

| Role | Description | Population |
|------|------------|-----------|
| **Mentee** | Community member seeking mentorship guidance | Unlimited |
| **Mentor** | Experienced AWS professional volunteering time | Unlimited |
| **Chapter_Lead** | Mentor with elevated meetup event management permissions | 1 per chapter (max 13) |
| **Super_Admin** | Platform administrator with full management access | Maximum 5 accounts |
| **System** | Automated processes (EventBridge scheduler, background jobs) | N/A — IAM roles |

## Data Access Matrix

### Users_Table (Classification: Confidential)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Authenticated User (own data) | ✅ | ✅ | ❌ (request deletion via admin) | userId in JWT matches record |
| Super_Admin | ✅ | ✅ (disable account) | ✅ (GDPR/privacy deletion) | Admin role verified, action audited |
| System (background jobs) | ✅ | ✅ (status updates) | ✅ (retention cleanup) | IAM role, scheduled execution |
| Other users | ❌ | ❌ | ❌ | — |

### Mentors_Table (Classification: Confidential)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Mentor (own profile) | ✅ | ✅ | ❌ | userId in JWT matches mentor.userId |
| Mentee (browsing) | ✅ (public fields only) | ❌ | ❌ | Authenticated, browse endpoint |
| Super_Admin | ✅ | ✅ | ✅ | Admin role, action audited |
| System | ✅ | ✅ (availability updates) | ✅ (retention cleanup) | IAM role |

### Mentees_Table (Classification: Confidential)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Mentee (own profile) | ✅ | ✅ | ❌ | userId in JWT matches mentee.userId |
| Matched Mentor | ✅ (goals, skills for plan generation) | ❌ | ❌ | Active session exists between pair |
| Super_Admin | ✅ | ✅ | ✅ | Admin role, action audited |
| System | ✅ | ✅ | ✅ (retention cleanup) | IAM role |

### Sessions_Table (Classification: Confidential)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Mentee (own sessions) | ✅ | ✅ (checklist, completion) | ❌ | menteeId in JWT matches |
| Mentor (own sessions) | ✅ | ✅ (accept/decline, completion) | ❌ | mentorId in JWT matches |
| Super_Admin | ✅ | ✅ (review/flag AI content) | ✅ | Admin role, action audited |
| System (plan generation) | ✅ | ✅ (plan persist, status updates) | ❌ | Content Lambda IAM role |

### Resume_Storage — S3 (Classification: Restricted)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Upload owner | ✅ (pre-signed URL) | ✅ (upload) | ✅ (via profile settings) | userId matches S3 key prefix |
| Matched Mentor | ✅ (pre-signed URL, read-only) | ❌ | ❌ | Active session exists |
| Super_Admin | ✅ | ❌ | ✅ (deletion requests) | Admin role, audited |
| System | ❌ | ❌ | ✅ (retention cleanup, Glacier lifecycle) | IAM role, scheduled |

### Analytics_Database — Aurora (Classification: Confidential)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Super_Admin | ✅ (dashboard aggregates) | ❌ | ❌ | Admin role |
| System (ETL jobs) | ✅ | ✅ | ✅ (retention pruning) | IAM DB auth via RDS Proxy |
| Regular users | ❌ | ❌ | ❌ | — |

### Secrets Manager (Classification: Restricted)

| Accessor | Read | Write | Delete | Condition |
|----------|------|-------|--------|-----------|
| Lambda functions | ✅ (cold start retrieval) | ❌ | ❌ | IAM role, permission boundary |
| Terraform (deploy time) | ✅ | ✅ | ✅ | Deploy role only |
| Humans | ❌ | ❌ | ❌ | No console access (SSO-based emergency break-glass only) |

## API Endpoint Access Control

| Endpoint Pattern | Mentee | Mentor | Chapter_Lead | Super_Admin |
|-----------------|--------|--------|--------------|-------------|
| `GET /v1/users/me` | ✅ | ✅ | ✅ | ✅ |
| `PUT /v1/users/me` | ✅ | ✅ | ✅ | ✅ |
| `GET /v1/mentors` (browse) | ✅ | ❌ | ❌ | ✅ |
| `POST /v1/sessions` (request mentor) | ✅ | ❌ | ❌ | ❌ |
| `PUT /v1/sessions/{id}/accept` | ❌ | ✅ | ✅ | ❌ |
| `PUT /v1/sessions/{id}/complete` | ✅ | ✅ | ✅ | ❌ |
| `GET /v1/sessions/{id}/plan` | ✅ (own) | ✅ (own) | ✅ (own) | ✅ (all) |
| `PUT /v1/admin/sessions/{id}/review` | ❌ | ❌ | ❌ | ✅ |
| `GET /v1/admin/users` | ❌ | ❌ | ❌ | ✅ |
| `PUT /v1/admin/users/{id}/disable` | ❌ | ❌ | ❌ | ✅ |
| `POST /v1/opportunities` | ❌ | ✅ | ✅ | ✅ |
| `POST /v1/events` (meetup management) | ❌ | ❌ | ✅ | ✅ |
| `GET /v1/admin/analytics` | ❌ | ❌ | ❌ | ✅ |
| `PUT /v1/admin/maintenance` | ❌ | ❌ | ❌ | ✅ |

## IAM Role Structure

| Lambda Role | Services Accessed | Boundary |
|-------------|------------------|----------|
| `{env}-guidedmentor-identity-lambda` | Cognito, DynamoDB (Users), Secrets Manager | ✅ |
| `{env}-guidedmentor-mentoring-lambda` | DynamoDB (Mentors, Mentees, Sessions, Opportunities), EventBridge | ✅ |
| `{env}-guidedmentor-content-lambda` | DynamoDB (Sessions), Bedrock, EventBridge, CloudWatch | ✅ |
| `{env}-guidedmentor-engagement-lambda` | DynamoDB (Notifications), AppSync, EventBridge | ✅ |
| `{env}-guidedmentor-analytics-lambda` | Aurora (RDS Proxy), DynamoDB (read), CloudWatch | ✅ |
| `{env}-guidedmentor-background-lambda` | DynamoDB (all tables), S3, EventBridge | ✅ |

## Review Schedule

This access control register is reviewed:
- Every 6 months as part of the regular security review cycle
- When new roles, endpoints, or data stores are introduced
- After any security incident involving access control
- When team members join or leave with administrative access
