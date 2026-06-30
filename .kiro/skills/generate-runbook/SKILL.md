---
name: generate-runbook
description: Generates a standardized incident runbook for a new CloudWatch alarm
inclusion: manual
---

# Generate Runbook

## Input Required
- Alarm name
- Trigger condition
- Severity (P1-P4)

## Template

Create `docs/runbooks/{alarm-name}.md`:
```
# Runbook: {Title}

## Alarm
- Name: `{env}-guidedmentor-{alarm}`
- Trigger: {condition}
- Severity: {P1-P4}

## Response Steps
1. Check CloudWatch Logs Insights
2. Identify affected service
3. Check recent deployments
4. Rollback if deploy-related
5. Escalate if infrastructure issue

## Resolution
- Confirm alarm returns to OK
- Document root cause
```
