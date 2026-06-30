# Runbook: DLQ Has Messages

## Alarm

**Name:** `{env}-guidedmentor-dlq-{queue}-has-messages`  
**Trigger:** DLQ `ApproximateNumberOfMessagesVisible` > 0  
**Severity:** P3 (escalate to P2 if messages accumulate without processing)

## DLQ Inventory

| DLQ Name | Source | Target Lambda |
|----------|--------|---------------|
| `{env}-guidedmentor-dlq-lock-cleanup` | EventBridge scheduled rule | Lock cleanup function |
| `{env}-guidedmentor-dlq-notification-digest` | EventBridge scheduled rule | Notification digest function |
| `{env}-guidedmentor-dlq-availability-reminder` | EventBridge scheduled rule | Availability reminder function |
| `{env}-guidedmentor-dlq-analytics-aggregation` | EventBridge scheduled rule | Analytics aggregation function |

## Response Steps

### 1. Identify Which DLQ

Check alarm name or query all DLQs:

```bash
for DLQ in lock-cleanup notification-digest availability-reminder analytics-aggregation; do
  echo "=== ${DLQ} ==="
  aws sqs get-queue-attributes \
    --queue-url https://sqs.{region}.amazonaws.com/{account}/{env}-guidedmentor-dlq-${DLQ} \
    --attribute-names ApproximateNumberOfMessages ApproximateNumberOfMessagesNotVisible
done
```

### 2. Sample Messages with `aws sqs receive-message`

```bash
aws sqs receive-message \
  --queue-url https://sqs.{region}.amazonaws.com/{account}/{env}-guidedmentor-dlq-{name} \
  --max-number-of-messages 5 \
  --visibility-timeout 0 \
  --message-attribute-names All
```

> **Important:** Use `--visibility-timeout 0` to peek without hiding messages from other consumers.

Examine the message body and attributes to understand:
- What event/payload was being processed
- When it was originally sent (`SentTimestamp` attribute)
- How many times it was received (`ApproximateReceiveCount`)

### 3. Check Target Lambda Logs for Failure Reason

```bash
aws logs filter-log-events \
  --log-group-name /aws/lambda/{env}-guidedmentor-{target-function} \
  --start-time $(date -u -d '1 hour ago' +%s)000 \
  --filter-pattern "ERROR" \
  --limit 20
```

Common failure reasons:
- **Timeout:** Function execution exceeded limit
- **Memory:** OOM kill (check `Runtime.ExitError` in logs)
- **Dependency failure:** DynamoDB/Bedrock unavailable
- **Code bug:** Unhandled exception in handler

### 4. Fix Root Cause

Based on findings:
- **Timeout:** Increase Lambda timeout or optimize code
- **Memory:** Increase Lambda memory allocation
- **Dependency:** Check dependent service health (see other runbooks)
- **Code bug:** Deploy fix, then replay messages

### 5. Replay Messages

Once the root cause is fixed, replay DLQ messages:

**Option A: Re-trigger via EventBridge (preferred for scheduled jobs)**

The scheduled rule will naturally re-run. If immediate replay is needed:

```bash
# Put the event back on EventBridge
aws events put-events \
  --entries '[{"Source":"guidedmentor.ops","DetailType":"DLQReplay","Detail":"{\"queue\":\"{dlq-name}\"}"}]'
```

**Option B: Move messages back to source queue**

```bash
# Receive from DLQ and send to source
aws sqs receive-message \
  --queue-url {dlq-url} \
  --max-number-of-messages 10 \
  --output json | jq -r '.Messages[] | .Body' | while read MSG; do
  aws sqs send-message \
    --queue-url {source-queue-url} \
    --message-body "${MSG}"
done
```

**Option C: Purge if messages are stale/irrelevant**

```bash
aws sqs purge-queue --queue-url {dlq-url}
```

> Only purge if messages are obsolete (e.g., old scheduled jobs that have since run successfully).

## Resolution

- Confirm DLQ message count returns to 0
- Alarm transitions to OK state
- Verify the target Lambda is processing successfully
- Document root cause and any configuration changes made
