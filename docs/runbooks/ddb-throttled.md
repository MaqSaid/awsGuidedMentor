# Runbook: DynamoDB Throttled Requests

## Alarm

**Name:** `{env}-guidedmentor-ddb-throttled`  
**Trigger:** DynamoDB throttled requests > 0  
**Severity:** P3 (escalate to P2 if sustained >5 minutes affecting user experience)

## Response Steps

### 1. Identify Which Table (Check Alarm Dimensions)

Check per-table throttling metrics:

```bash
for TABLE in users mentors mentees sessions notifications; do
  echo "=== ${TABLE} ==="
  aws cloudwatch get-metric-statistics \
    --namespace AWS/DynamoDB \
    --metric-name ThrottledRequests \
    --dimensions Name=TableName,Value={env}-guidedmentor-${TABLE} \
    --start-time $(date -u -d '15 minutes ago' +%Y-%m-%dT%H:%M:%S) \
    --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
    --period 60 \
    --statistics Sum
done
```

### 2. Check Consumed vs Provisioned Capacity

```bash
aws dynamodb describe-table \
  --table-name {env}-guidedmentor-{table} \
  --query 'Table.{BillingMode:BillingModeSummary.BillingMode,RCU:ProvisionedThroughput.ReadCapacityUnits,WCU:ProvisionedThroughput.WriteCapacityUnits}'
```

### 3. Tables Use On-Demand — Throttling Indicates Hot Partition

All GuidedMentor tables use `PAY_PER_REQUEST` (on-demand) billing mode. With on-demand:
- There is no provisioned capacity to exhaust
- Throttling means you're hitting **partition-level limits** (3,000 RCU / 1,000 WCU per partition)
- This indicates a **hot partition key** problem

### 4. Review Access Patterns for Hot Key

Common hot partition scenarios in GuidedMentor:
- **Users table:** Many reads for the same popular mentor profile
- **Sessions table:** Burst of writes for a single mentor's sessions
- **Notifications table:** Batch writes all targeting same user partition

Check CloudWatch Contributor Insights (if enabled):
```bash
aws dynamodb describe-contributor-insights \
  --table-name {env}-guidedmentor-{table}
```

### 5. Short-Term: Retry Handles It; Long-Term: Fix Partition Key Distribution

**Immediate (no action needed):**
- The application uses Polly retry policies with exponential backoff
- Transient throttling resolves automatically
- DynamoDB SDK retries handle brief bursts

**Long-term (if recurring):**
- Review partition key design for affected table
- Consider adding a random suffix to partition keys (write sharding)
- Use GSI with different partition key for hot read patterns
- Enable DynamoDB Contributor Insights to identify the exact hot key

## Resolution

- Confirm throttling returns to 0
- Alarm transitions to OK state
- If recurring: create a ticket to investigate partition key distribution
- Document the hot key pattern for future reference
