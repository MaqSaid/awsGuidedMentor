# Information Classification Matrix

> GuidedMentor Platform — ISO/IEC 27001 A.8.2 Compliance
>
> Last reviewed: 2025-01-15 | Next review: 2025-07-15

## Classification Levels

| Level | Definition | Handling Requirements |
|-------|-----------|----------------------|
| **Public** | Information intended for public consumption | No restrictions on distribution |
| **Internal** | Platform operational data not intended for external sharing | Accessible to authenticated users only |
| **Confidential** | User-provided personal data requiring protection | Encrypted at rest and in transit, access logged |
| **Restricted** | Highly sensitive data with strict access controls | CMK encryption, permission-boundary-protected access, full audit trail |

## Data Store Classification

| Data Store | Classification | Justification | Encryption | Retention |
|-----------|---------------|---------------|------------|-----------|
| **Users_Table** (DynamoDB) | Confidential | Contains user PII: email, name, active role, profile data | AWS KMS (CMK in staging/prod, AWS-managed in dev) | 3 years after last activity |
| **Mentors_Table** (DynamoDB) | Confidential | Contains mentor professional profiles, availability, expertise | AWS KMS (CMK in staging/prod) | 3 years after last activity |
| **Mentees_Table** (DynamoDB) | Confidential | Contains mentee goals, skills, preferences | AWS KMS (CMK in staging/prod) | 3 years after last activity |
| **Sessions_Table** (DynamoDB) | Confidential | Contains AI-generated session plans, completion status, model versions | AWS KMS (CMK in staging/prod) | 3 years after last activity |
| **Notifications_Table** (DynamoDB) | Internal | Contains notification delivery records, read/unread status | AWS KMS (CMK in staging/prod) | 90 days (auto-purge via TTL) |
| **Opportunities_Table** (DynamoDB) | Internal | Contains job/event postings by mentors | AWS KMS (CMK in staging/prod) | Until expired (TTL on expiresAt) |
| **Analytics_Database** (Aurora PostgreSQL) | Confidential | Aggregated analytics, cross-entity joins, engagement data | KMS encryption at rest, TLS 1.3 in transit | 3 years rolling |
| **Resume_Storage** (S3) | Restricted | Contains user-uploaded resumes with PII (names, addresses, employment history) | AES-256 (SSE-S3), CMK in staging/prod | Deleted on user request within 30 days; otherwise 3 years after last activity |
| **CloudWatch Logs** | Internal | Structured application logs (may contain correlationIds, userIds) | CloudWatch default encryption | 90 days hot, 365 days cold (Glacier) |
| **Audit Logs** | Restricted | Every state change: userId, timestamp, action, resource, correlationId | KMS encryption | 7 years (regulatory compliance) |
| **Cognito User Pool** | Restricted | Authentication credentials, MFA tokens, OAuth tokens | AWS-managed encryption | Until account deletion |
| **Secrets Manager** | Restricted | API keys, database credentials, Bedrock model IDs | AWS KMS envelope encryption | Rotated quarterly |
| **Bedrock Invocation Logs** | Confidential | Input/output hashes, model versions, latency metrics, token counts | CloudWatch encryption | 365 days |

## Data in Transit

| Flow | Classification | Protection |
|------|---------------|-----------|
| Browser → CloudFront → API Gateway | Confidential | TLS 1.3, HSTS (max-age 31536000) |
| API Gateway → Lambda | Internal | AWS internal networking, IAM auth |
| Lambda → DynamoDB | Confidential | TLS 1.3, IAM role-based access |
| Lambda → Bedrock | Confidential | TLS 1.3, IAM role, VPC endpoint (prod) |
| Lambda → Aurora | Confidential | TLS 1.3, IAM DB auth, RDS Proxy |
| AppSync → Client (GraphQL WS) | Internal | WSS (TLS 1.3), Cognito auth |

## Labelling and Marking

- All DynamoDB tables tagged with `DataClassification` tag matching this matrix
- S3 buckets tagged with `DataClassification: restricted`
- CloudWatch log groups tagged with `DataClassification: internal` or `restricted`
- Terraform resource tags enforce classification labels at infrastructure level

## Review Schedule

This classification matrix is reviewed:
- Every 6 months as part of the regular security review cycle
- Whenever a new data store or data flow is introduced
- When data handling requirements change (regulatory or business-driven)
