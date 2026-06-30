# Incident Response Playbook

## Severity Levels

| Level | Description | Response Time | Examples |
|-------|-------------|---------------|----------|
| **P1** | Critical — platform unusable for all users | 15 minutes | Complete API failure, data loss, security breach |
| **P2** | High — major feature broken for many users | 30 minutes | Authentication down, session creation failing, sustained high error rate |
| **P3** | Medium — degraded experience, workaround exists | 2 hours | High latency, single DLQ accumulating, Bedrock temporarily unavailable |
| **P4** | Low — minor issue, no user impact | Next business day | Alarm flapping, non-critical background job delayed |

## Escalation Paths

### P1 — Critical
1. On-call engineer acknowledges within 15 minutes
2. Notify team lead immediately
3. Open a war room (Slack channel / Teams call)
4. Communicate to stakeholders within 30 minutes
5. All hands until resolved

### P2 — High
1. On-call engineer acknowledges within 30 minutes
2. Investigate and mitigate within 1 hour
3. Notify team lead if not resolved in 1 hour
4. Stakeholder update if impact >1 hour

### P3 — Medium
1. On-call engineer acknowledges within 2 hours
2. Investigate during business hours
3. Track in incident log

### P4 — Low
1. Log in backlog
2. Address in next sprint or maintenance window

## Incident Response Process

### 1. Detect & Acknowledge

- CloudWatch alarm fires → SNS notification → on-call engineer
- Acknowledge in monitoring system
- Create incident ticket with severity level

### 2. Assess & Communicate

**Initial assessment (first 5 minutes):**
- What is the user impact?
- Is it getting worse?
- What is the blast radius?

**Communication template (internal):**
```
INCIDENT: {severity} — {title}
STATUS: Investigating / Identified / Mitigating / Resolved
IMPACT: {description of user impact}
STARTED: {timestamp}
LEAD: {engineer name}
NEXT UPDATE: {time}
```

### 3. Mitigate

Priority order:
1. **Restore service** (rollback, failover, scale) — don't debug first
2. **Reduce blast radius** (feature flags, traffic shifting)
3. **Communicate** (status page, stakeholders)
4. **Investigate root cause** (after service is stable)

### 4. Resolve

- Confirm all alarms return to OK
- Verify user-facing functionality is restored
- Remove any temporary mitigations (e.g., provisioned concurrency)
- Final stakeholder communication

### 5. Post-Mortem

Conduct within 48 hours of P1/P2 incidents.

**Post-mortem template:**

```markdown
## Incident Post-Mortem: {title}

**Date:** {date}
**Duration:** {start} — {end} ({total time})
**Severity:** {P1/P2/P3}
**Lead:** {engineer}

### Summary
{1-2 sentence description of what happened}

### Timeline
- {HH:MM} — Alarm triggered
- {HH:MM} — Engineer acknowledged
- {HH:MM} — Root cause identified
- {HH:MM} — Mitigation applied
- {HH:MM} — Service restored

### Root Cause
{Technical explanation of why the incident occurred}

### Impact
- Users affected: {count/percentage}
- Duration of impact: {minutes}
- Data loss: {none/description}

### What Went Well
- {positive aspects of response}

### What Could Be Improved
- {areas for improvement}

### Action Items
| Action | Owner | Due Date | Priority |
|--------|-------|----------|----------|
| {action} | {name} | {date} | {P1-P4} |
```

## Useful Commands (Quick Reference)

```bash
# Check all Lambda function errors in last 30 minutes
for FN in identity mentoring content engagement; do
  echo "=== ${FN} ==="
  aws cloudwatch get-metric-statistics \
    --namespace AWS/Lambda --metric-name Errors \
    --dimensions Name=FunctionName,Value={env}-guidedmentor-${FN} \
    --start-time $(date -u -d '30 minutes ago' +%Y-%m-%dT%H:%M:%S) \
    --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
    --period 300 --statistics Sum
done

# Rollback a Lambda to previous version
aws lambda update-alias \
  --function-name {env}-guidedmentor-{context} \
  --name live \
  --function-version {previous-version}

# Check DynamoDB table status
aws dynamodb describe-table \
  --table-name {env}-guidedmentor-{table} \
  --query 'Table.TableStatus'

# Check all DLQ depths
for DLQ in lock-cleanup notification-digest availability-reminder analytics-aggregation; do
  echo "=== ${DLQ} ==="
  aws sqs get-queue-attributes \
    --queue-url https://sqs.{region}.amazonaws.com/{account}/{env}-guidedmentor-dlq-${DLQ} \
    --attribute-names ApproximateNumberOfMessages
done
```

## Related Runbooks

- [High Error Rate](./high-error-rate.md)
- [High Latency](./high-latency.md)
- [Bedrock Failures](./bedrock-failures.md)
- [DynamoDB Throttled](./ddb-throttled.md)
- [DLQ Messages](./dlq-messages.md)
- [Chaos Testing Guide](./chaos-testing-guide.md)
