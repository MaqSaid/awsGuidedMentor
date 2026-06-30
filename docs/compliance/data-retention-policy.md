# Data Retention Policy

> GuidedMentor Platform — ISO/IEC 27001 A.12.4 Compliance
>
> Last reviewed: 2025-01-15 | Next review: 2025-07-15

## Policy Overview

This policy defines the retention periods for all data within the GuidedMentor platform, the mechanisms for enforcement, and the procedures for data deletion upon user request.

## Retention Periods

| Data Category | Retention Period | Trigger for Deletion | Mechanism |
|--------------|-----------------|---------------------|-----------|
| User account data (Users_Table) | 3 years after last activity | `lastActivityAt` + 3 years | Scheduled background job |
| Mentor profiles (Mentors_Table) | 3 years after last activity | Linked user's `lastActivityAt` + 3 years | Scheduled background job |
| Mentee profiles (Mentees_Table) | 3 years after last activity | Linked user's `lastActivityAt` + 3 years | Scheduled background job |
| Session plans (Sessions_Table) | 3 years after last activity | Last update or completion + 3 years | Scheduled background job |
| Notifications (Notifications_Table) | 90 days | DynamoDB TTL on `expiresAt` | Automatic (DynamoDB TTL) |
| Opportunity postings (Opportunities_Table) | Until expiry + 30 days | DynamoDB TTL on `expiresAt` | Automatic (DynamoDB TTL) |
| Resumes (S3) | 3 years after last activity, or on request | Lifecycle rule + deletion job | S3 lifecycle to Glacier at 30 days; deletion job |
| Analytics data (Aurora) | 3 years rolling | Partition pruning on date | Scheduled ETL cleanup |
| Application logs (CloudWatch) | 90 days hot / 365 days cold | Log group retention policy | CloudWatch retention settings |
| Audit logs | 7 years | Regulatory requirement | CloudWatch + S3 archive |
| Bedrock invocation logs | 365 days | CloudWatch retention | CloudWatch retention settings |

## Deletion on User Request

### Process

1. User submits a deletion request through the platform settings page
2. The request is recorded in an audit log with timestamp and correlationId
3. A Super_Admin is notified of the pending deletion request
4. Within **30 calendar days**, all user data is permanently deleted:
   - Users_Table record
   - Associated Mentors_Table record (if mentor profile exists)
   - Associated Mentees_Table record (if mentee profile exists)
   - All Sessions_Table records where user is mentor or mentee
   - Resume file in S3 (if uploaded)
   - Analytics records anonymised (userId replaced with hash)
   - Cognito user pool record deleted
5. Confirmation email sent to user's registered email address
6. Deletion completion recorded in audit log

### Technical Implementation

```
EventBridge Rule: "user-deletion-request"
  → Lambda: DataDeletionHandler
  → Steps:
    1. Mark user as "pending_deletion" in Users_Table
    2. Queue deletion tasks for each data store
    3. Execute deletions in parallel (DynamoDB batch writes)
    4. Anonymise Aurora analytics records
    5. Delete S3 objects with matching userId prefix
    6. Invoke Cognito AdminDeleteUser
    7. Publish "user-deletion-complete" event
    8. Send confirmation notification
```

### Constraints

- Deletion requests are irrevocable once confirmed by the user
- Active sessions must be resolved or cancelled before deletion proceeds
- Audit logs of the deletion action itself are retained for 7 years (regulatory)
- Anonymised analytics data is retained (no PII, only statistical aggregates)

## Automatic Retention Enforcement

### Background Job: `RetentionCleanupJob`

- **Schedule**: Daily at 02:00 AEST (via EventBridge Scheduler)
- **Logic**:
  1. Scan Users_Table for records where `lastActivityAt` < (now - 3 years)
  2. For each stale user:
     - Send 30-day warning notification (if not already sent)
     - After 30-day grace period: execute full deletion flow
  3. Log all actions to audit trail

### DynamoDB TTL-Based Cleanup

- Notifications: `expiresAt` attribute set to `createdAt + 90 days`
- Opportunity postings: `expiresAt` set by creator (max 90 days)
- Session locks: `lockExpiresAt` set to creation + 15 minutes

### S3 Lifecycle Rules

- Resumes: Transition to Glacier after 30 days of no access
- Resumes: Permanent deletion after retention period expires
- Versioned objects: Non-current versions deleted after 7 days

## Activity Tracking

"Last activity" is defined as any of the following actions by the user:
- Signing in to the platform
- Updating profile information
- Interacting with a session (checklist update, completion)
- Sending or responding to a mentorship request
- Posting an opportunity
- Any API call authenticated with the user's JWT

The `lastActivityAt` field is updated in the Users_Table on each qualifying action.

## Review Schedule

This data retention policy is reviewed:
- Annually as part of the compliance review cycle
- When privacy legislation changes (Australian Privacy Act, GDPR applicability)
- When new data categories are introduced to the platform
