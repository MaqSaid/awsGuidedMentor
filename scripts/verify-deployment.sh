#!/bin/bash
# ==============================================================================
# GuidedMentor Platform — Deployment Verification Script
# ==============================================================================
# Validates that all integration points are correctly wired after deployment.
# Usage: ./scripts/verify-deployment.sh [environment]
#   environment: dev (default), staging, prod
#
# Prerequisites:
#   - AWS CLI configured with appropriate credentials
#   - jq installed for JSON parsing
#   - curl available for HTTP checks
# ==============================================================================

set -euo pipefail

ENV="${1:-dev}"
REGION="ap-southeast-2"
NAME_PREFIX="${ENV}-guidedmentor"
PASS_COUNT=0
FAIL_COUNT=0
WARN_COUNT=0

# Colours for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

pass() {
  echo -e "  ${GREEN}✓${NC} $1"
  ((PASS_COUNT++))
}

fail() {
  echo -e "  ${RED}✗${NC} $1"
  ((FAIL_COUNT++))
}

warn() {
  echo -e "  ${YELLOW}⚠${NC} $1"
  ((WARN_COUNT++))
}

section() {
  echo -e "\n${BLUE}━━━ $1 ━━━${NC}"
}

# ==============================================================================
section "1. CloudFront & SPA Assets"
# ==============================================================================

# Get CloudFront distribution domain
CF_DOMAIN=$(aws cloudfront list-distributions \
  --query "DistributionList.Items[?Comment=='${ENV} GuidedMentor SPA Distribution'].DomainName | [0]" \
  --output text 2>/dev/null || echo "NOT_FOUND")

if [ "$CF_DOMAIN" != "NOT_FOUND" ] && [ "$CF_DOMAIN" != "None" ]; then
  pass "CloudFront distribution found: ${CF_DOMAIN}"

  # Check index.html is served
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "https://${CF_DOMAIN}/" 2>/dev/null || echo "000")
  if [ "$HTTP_CODE" = "200" ]; then
    pass "CloudFront serves index.html (HTTP 200)"
  else
    fail "CloudFront returned HTTP ${HTTP_CODE} for index.html"
  fi

  # Check cache headers
  CACHE_HEADER=$(curl -s -I "https://${CF_DOMAIN}/index.html" 2>/dev/null | grep -i "cache-control" || echo "")
  if echo "$CACHE_HEADER" | grep -qi "no-cache"; then
    pass "index.html has no-cache header (correct for SPA entry point)"
  else
    warn "index.html cache-control header may not be configured: ${CACHE_HEADER}"
  fi

  # Check security headers
  SECURITY_HEADERS=$(curl -s -I "https://${CF_DOMAIN}/" 2>/dev/null)
  if echo "$SECURITY_HEADERS" | grep -qi "x-frame-options"; then
    pass "X-Frame-Options security header present"
  else
    fail "X-Frame-Options security header missing"
  fi
  if echo "$SECURITY_HEADERS" | grep -qi "strict-transport-security"; then
    pass "HSTS header present"
  else
    fail "HSTS header missing"
  fi
  if echo "$SECURITY_HEADERS" | grep -qi "content-security-policy"; then
    pass "Content-Security-Policy header present"
  else
    fail "Content-Security-Policy header missing"
  fi

  # Check remote entry files exist
  for remote in identity mentoring content engagement; do
    REMOTE_CODE=$(curl -s -o /dev/null -w "%{http_code}" "https://${CF_DOMAIN}/remotes/${remote}/assets/remoteEntry.js" 2>/dev/null || echo "000")
    if [ "$REMOTE_CODE" = "200" ]; then
      pass "Module Federation remote '${remote}' remoteEntry.js accessible"
    else
      fail "Module Federation remote '${remote}' remoteEntry.js not found (HTTP ${REMOTE_CODE})"
    fi
  done
else
  warn "CloudFront distribution not found for environment '${ENV}' (expected for local dev)"
fi

# ==============================================================================
section "2. API Gateway & Lambda Endpoints"
# ==============================================================================

# Get API Gateway URL from Terraform output or directly
API_URL=$(aws apigateway get-rest-apis \
  --query "items[?name=='${NAME_PREFIX}-api'].id | [0]" \
  --output text 2>/dev/null || echo "NOT_FOUND")

if [ "$API_URL" != "NOT_FOUND" ] && [ "$API_URL" != "None" ]; then
  API_INVOKE_URL="https://${API_URL}.execute-api.${REGION}.amazonaws.com/${ENV}"
  pass "API Gateway found: ${API_INVOKE_URL}"

  # Health check endpoints for all 4 contexts
  for context in identity mentoring content engagement; do
    FUNC_NAME="${NAME_PREFIX}-${context}"
    FUNC_EXISTS=$(aws lambda get-function --function-name "${FUNC_NAME}" --query "Configuration.FunctionName" --output text 2>/dev/null || echo "NOT_FOUND")
    if [ "$FUNC_EXISTS" != "NOT_FOUND" ]; then
      pass "Lambda function '${FUNC_NAME}' exists"
      
      # Check function state
      FUNC_STATE=$(aws lambda get-function --function-name "${FUNC_NAME}" --query "Configuration.State" --output text 2>/dev/null || echo "UNKNOWN")
      if [ "$FUNC_STATE" = "Active" ]; then
        pass "  └─ State: Active"
      else
        fail "  └─ State: ${FUNC_STATE} (expected Active)"
      fi
    else
      fail "Lambda function '${FUNC_NAME}' not found"
    fi
  done

  # Check Cognito authorizer is configured
  AUTHORIZERS=$(aws apigateway get-authorizers --rest-api-id "${API_URL}" --query "items[?type=='COGNITO_USER_POOLS'].name" --output json 2>/dev/null || echo "[]")
  if echo "$AUTHORIZERS" | grep -q "cognito"; then
    pass "Cognito JWT authorizer configured on API Gateway"
  else
    fail "Cognito JWT authorizer not found on API Gateway"
  fi
else
  warn "API Gateway not found for environment '${ENV}' (expected for local dev)"
fi

# ==============================================================================
section "3. DynamoDB Tables"
# ==============================================================================

REQUIRED_TABLES=(
  "${NAME_PREFIX}-users"
  "${NAME_PREFIX}-mentors"
  "${NAME_PREFIX}-mentees"
  "${NAME_PREFIX}-sessions"
  "${NAME_PREFIX}-notifications"
  "${NAME_PREFIX}-meetups"
  "${NAME_PREFIX}-engagement-events"
  "${NAME_PREFIX}-opportunities"
)

for table in "${REQUIRED_TABLES[@]}"; do
  TABLE_STATUS=$(aws dynamodb describe-table --table-name "${table}" --query "Table.TableStatus" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$TABLE_STATUS" = "ACTIVE" ]; then
    pass "DynamoDB table '${table}' is ACTIVE"
    
    # Check PITR is enabled
    PITR=$(aws dynamodb describe-continuous-backups --table-name "${table}" --query "ContinuousBackupsDescription.PointInTimeRecoveryDescription.PointInTimeRecoveryStatus" --output text 2>/dev/null || echo "UNKNOWN")
    if [ "$PITR" = "ENABLED" ]; then
      pass "  └─ Point-in-time recovery: ENABLED"
    else
      warn "  └─ Point-in-time recovery: ${PITR}"
    fi
  elif [ "$TABLE_STATUS" = "NOT_FOUND" ]; then
    fail "DynamoDB table '${table}' does not exist"
  else
    warn "DynamoDB table '${table}' status: ${TABLE_STATUS}"
  fi
done

# Check DynamoDB Streams on application tables (required for Aurora replication)
if [ "$ENV" != "dev" ]; then
  for table in "${NAME_PREFIX}-users" "${NAME_PREFIX}-mentors" "${NAME_PREFIX}-sessions"; do
    STREAM_STATUS=$(aws dynamodb describe-table --table-name "${table}" --query "Table.StreamSpecification.StreamEnabled" --output text 2>/dev/null || echo "false")
    if [ "$STREAM_STATUS" = "True" ]; then
      pass "DynamoDB Streams enabled on '${table}'"
    else
      fail "DynamoDB Streams NOT enabled on '${table}' (required for Aurora replication)"
    fi
  done
fi

# ==============================================================================
section "4. AppSync (Real-Time Notifications)"
# ==============================================================================

APPSYNC_API=$(aws appsync list-graphql-apis \
  --query "graphqlApis[?name=='${NAME_PREFIX}-notifications'].apiId | [0]" \
  --output text 2>/dev/null || echo "NOT_FOUND")

if [ "$APPSYNC_API" != "NOT_FOUND" ] && [ "$APPSYNC_API" != "None" ]; then
  pass "AppSync GraphQL API found: ${APPSYNC_API}"

  # Check authentication mode
  AUTH_TYPE=$(aws appsync get-graphql-api --api-id "${APPSYNC_API}" --query "graphqlApi.authenticationType" --output text 2>/dev/null || echo "UNKNOWN")
  if [ "$AUTH_TYPE" = "AMAZON_COGNITO_USER_POOLS" ]; then
    pass "AppSync uses Cognito authentication"
  else
    warn "AppSync auth type: ${AUTH_TYPE} (expected AMAZON_COGNITO_USER_POOLS)"
  fi

  # Check WebSocket endpoint exists
  WS_URL=$(aws appsync get-graphql-api --api-id "${APPSYNC_API}" --query "graphqlApi.uris.REALTIME" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$WS_URL" != "NOT_FOUND" ] && [ "$WS_URL" != "None" ]; then
    pass "AppSync realtime WebSocket endpoint: ${WS_URL}"
  else
    fail "AppSync realtime WebSocket endpoint not found"
  fi
else
  warn "AppSync API not found for environment '${ENV}'"
fi

# ==============================================================================
section "5. EventBridge Scheduler"
# ==============================================================================

EXPECTED_SCHEDULES=(
  "${NAME_PREFIX}-lock-cleanup"
  "${NAME_PREFIX}-notification-digest"
  "${NAME_PREFIX}-availability-reminder"
)

for schedule in "${EXPECTED_SCHEDULES[@]}"; do
  SCHED_STATE=$(aws scheduler get-schedule --name "${schedule}" --query "State" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$SCHED_STATE" = "ENABLED" ]; then
    pass "EventBridge schedule '${schedule}' is ENABLED"
  elif [ "$SCHED_STATE" = "NOT_FOUND" ]; then
    fail "EventBridge schedule '${schedule}' not found"
  else
    warn "EventBridge schedule '${schedule}' state: ${SCHED_STATE}"
  fi
done

# Analytics aggregation only runs when Aurora is enabled (staging/prod)
if [ "$ENV" != "dev" ]; then
  ANALYTICS_SCHED=$(aws scheduler get-schedule --name "${NAME_PREFIX}-analytics-aggregation" --query "State" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$ANALYTICS_SCHED" = "ENABLED" ]; then
    pass "EventBridge schedule '${NAME_PREFIX}-analytics-aggregation' is ENABLED"
  else
    fail "EventBridge schedule '${NAME_PREFIX}-analytics-aggregation' not found (required for staging/prod)"
  fi
fi

# Check custom event bus
EVENT_BUS=$(aws events describe-event-bus --name "${NAME_PREFIX}" --query "Name" --output text 2>/dev/null || echo "NOT_FOUND")
if [ "$EVENT_BUS" != "NOT_FOUND" ]; then
  pass "Custom EventBridge bus '${NAME_PREFIX}' exists"
else
  fail "Custom EventBridge bus '${NAME_PREFIX}' not found"
fi

# Check DLQs exist
DLQS=(
  "${NAME_PREFIX}-dlq-lock-cleanup"
  "${NAME_PREFIX}-dlq-notification-digest"
  "${NAME_PREFIX}-dlq-availability-reminder"
)

for dlq in "${DLQS[@]}"; do
  DLQ_URL=$(aws sqs get-queue-url --queue-name "${dlq}" --query "QueueUrl" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$DLQ_URL" != "NOT_FOUND" ]; then
    pass "SQS DLQ '${dlq}' exists"
  else
    fail "SQS DLQ '${dlq}' not found"
  fi
done

# ==============================================================================
section "6. Aurora PostgreSQL (Staging/Prod Only)"
# ==============================================================================

if [ "$ENV" != "dev" ]; then
  CLUSTER_ID="${NAME_PREFIX}-analytics"
  CLUSTER_STATUS=$(aws rds describe-db-clusters --db-cluster-identifier "${CLUSTER_ID}" --query "DBClusters[0].Status" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$CLUSTER_STATUS" = "available" ]; then
    pass "Aurora cluster '${CLUSTER_ID}' is available"
    
    # Check RDS Proxy
    PROXY_STATUS=$(aws rds describe-db-proxies --db-proxy-name "${NAME_PREFIX}-proxy" --query "DBProxies[0].Status" --output text 2>/dev/null || echo "NOT_FOUND")
    if [ "$PROXY_STATUS" = "available" ]; then
      pass "RDS Proxy '${NAME_PREFIX}-proxy' is available"
    else
      warn "RDS Proxy status: ${PROXY_STATUS}"
    fi
  else
    fail "Aurora cluster '${CLUSTER_ID}' status: ${CLUSTER_STATUS}"
  fi
else
  pass "Aurora PostgreSQL correctly SKIPPED for dev environment"
fi

# ==============================================================================
section "7. Cognito User Pool"
# ==============================================================================

POOL_ID=$(aws cognito-idp list-user-pools --max-results 20 \
  --query "UserPools[?Name=='${NAME_PREFIX}-users'].Id | [0]" \
  --output text 2>/dev/null || echo "NOT_FOUND")

if [ "$POOL_ID" != "NOT_FOUND" ] && [ "$POOL_ID" != "None" ]; then
  pass "Cognito User Pool found: ${POOL_ID}"

  # Check Google IdP is configured
  IDP_LIST=$(aws cognito-idp list-identity-providers --user-pool-id "${POOL_ID}" --query "Providers[].ProviderName" --output json 2>/dev/null || echo "[]")
  if echo "$IDP_LIST" | grep -qi "google"; then
    pass "Google OAuth IdP configured"
  else
    warn "Google OAuth IdP not configured (check google_client_id in tfvars)"
  fi
else
  fail "Cognito User Pool not found"
fi

# ==============================================================================
section "8. S3 Buckets"
# ==============================================================================

for bucket in "${NAME_PREFIX}-spa" "${NAME_PREFIX}-resumes"; do
  BUCKET_EXISTS=$(aws s3api head-bucket --bucket "${bucket}" 2>&1)
  if [ $? -eq 0 ]; then
    pass "S3 bucket '${bucket}' exists and is accessible"
    
    # Check public access block
    PUBLIC_BLOCK=$(aws s3api get-public-access-block --bucket "${bucket}" --query "PublicAccessBlockConfiguration.BlockPublicAcls" --output text 2>/dev/null || echo "false")
    if [ "$PUBLIC_BLOCK" = "True" ]; then
      pass "  └─ Public access blocked"
    else
      fail "  └─ Public access NOT blocked"
    fi
  else
    fail "S3 bucket '${bucket}' not found or not accessible"
  fi
done

# ==============================================================================
section "9. Security (WAF, KMS) — Staging/Prod Only"
# ==============================================================================

if [ "$ENV" != "dev" ]; then
  # Check WAF Web ACL
  WAF_ACL=$(aws wafv2 list-web-acls --scope REGIONAL \
    --query "WebACLs[?Name=='${NAME_PREFIX}-waf'].Id | [0]" \
    --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$WAF_ACL" != "NOT_FOUND" ] && [ "$WAF_ACL" != "None" ]; then
    pass "WAF Web ACL found: ${WAF_ACL}"
  else
    fail "WAF Web ACL not found (required for staging/prod)"
  fi

  # Check KMS CMK
  KMS_ALIAS=$(aws kms list-aliases --query "Aliases[?AliasName=='alias/${NAME_PREFIX}-cmk'].TargetKeyId | [0]" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$KMS_ALIAS" != "NOT_FOUND" ] && [ "$KMS_ALIAS" != "None" ]; then
    pass "KMS CMK found via alias"
  else
    fail "KMS CMK alias 'alias/${NAME_PREFIX}-cmk' not found"
  fi
else
  pass "WAF/KMS correctly SKIPPED for dev environment"
fi

# ==============================================================================
section "10. Background Job Lambda Functions"
# ==============================================================================

BG_FUNCTIONS=(
  "${NAME_PREFIX}-lock-cleanup"
  "${NAME_PREFIX}-notification-digest"
  "${NAME_PREFIX}-availability-reminder"
  "${NAME_PREFIX}-opportunity-expiry"
  "${NAME_PREFIX}-completion-reminder"
  "${NAME_PREFIX}-escalation"
)

for func in "${BG_FUNCTIONS[@]}"; do
  FUNC_STATE=$(aws lambda get-function --function-name "${func}" --query "Configuration.State" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$FUNC_STATE" = "Active" ]; then
    pass "Background Lambda '${func}' is Active"
  elif [ "$FUNC_STATE" = "NOT_FOUND" ]; then
    warn "Background Lambda '${func}' not found (may not be deployed yet)"
  else
    fail "Background Lambda '${func}' state: ${FUNC_STATE}"
  fi
done

# DynamoDB Stream replication function (staging/prod only)
if [ "$ENV" != "dev" ]; then
  REPL_FUNC="${NAME_PREFIX}-ddb-stream-replication"
  REPL_STATE=$(aws lambda get-function --function-name "${REPL_FUNC}" --query "Configuration.State" --output text 2>/dev/null || echo "NOT_FOUND")
  if [ "$REPL_STATE" = "Active" ]; then
    pass "DynamoDB Stream replication Lambda '${REPL_FUNC}' is Active"
  else
    fail "DynamoDB Stream replication Lambda '${REPL_FUNC}' not found (required for Aurora replication)"
  fi
fi

# ==============================================================================
section "Summary"
# ==============================================================================

echo ""
echo -e "  ${GREEN}Passed:${NC}  ${PASS_COUNT}"
echo -e "  ${RED}Failed:${NC}  ${FAIL_COUNT}"
echo -e "  ${YELLOW}Warnings:${NC} ${WARN_COUNT}"
echo ""

if [ "$FAIL_COUNT" -gt 0 ]; then
  echo -e "${RED}⚠ Deployment verification FAILED with ${FAIL_COUNT} error(s).${NC}"
  echo "  Review the failures above and fix before proceeding."
  exit 1
else
  echo -e "${GREEN}✓ Deployment verification PASSED.${NC}"
  if [ "$WARN_COUNT" -gt 0 ]; then
    echo -e "  ${YELLOW}(${WARN_COUNT} warnings — review for completeness)${NC}"
  fi
  exit 0
fi
