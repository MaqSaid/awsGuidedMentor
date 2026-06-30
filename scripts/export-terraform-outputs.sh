#!/bin/bash
# ==============================================================================
# Export Terraform Outputs as Environment Variables
# ==============================================================================
# Extracts Terraform outputs and formats them for use in:
#   - GitHub Actions vars (for CI/CD workflows)
#   - Local .env files (for frontend development)
#   - Lambda environment variables (for backend services)
#
# Usage: ./scripts/export-terraform-outputs.sh [environment] [format]
#   environment: dev (default), staging, prod
#   format: env (default), github, json
# ==============================================================================

set -euo pipefail

ENV="${1:-dev}"
FORMAT="${2:-env}"

cd infrastructure

# Initialize Terraform if needed
terraform init -backend-config="key=${ENV}/terraform.tfstate" > /dev/null 2>&1

# Extract outputs
echo "Extracting Terraform outputs for environment: ${ENV}" >&2

API_GATEWAY_URL=$(terraform output -raw networking_outputs 2>/dev/null | jq -r '.api_gateway_url // empty' || echo "")
CLOUDFRONT_DOMAIN=$(terraform output -raw networking_outputs 2>/dev/null | jq -r '.cloudfront_domain // empty' || echo "")
CLOUDFRONT_DIST_ID=$(terraform output -raw networking_outputs 2>/dev/null | jq -r '.cloudfront_distribution_id // empty' || echo "")
SPA_BUCKET=$(terraform output -raw networking_outputs 2>/dev/null | jq -r '.spa_bucket_name // empty' || echo "")
USER_POOL_ID=$(terraform output -raw identity_outputs 2>/dev/null | jq -r '.user_pool_id // empty' || echo "")
CLIENT_ID=$(terraform output -raw identity_outputs 2>/dev/null | jq -r '.client_id // empty' || echo "")
APPSYNC_URL=$(terraform output -raw engagement_outputs 2>/dev/null | jq -r '.appsync_url // empty' || echo "")
EVENT_BUS_NAME=$(terraform output -raw events_outputs 2>/dev/null | jq -r '.event_bus_name // empty' || echo "")

case "$FORMAT" in
  env)
    # .env format for local development
    cat <<EOF
# Auto-generated from Terraform outputs (${ENV})
# Generated: $(date -u +"%Y-%m-%dT%H:%M:%SZ")
VITE_API_URL=${API_GATEWAY_URL}
VITE_COGNITO_USER_POOL_ID=${USER_POOL_ID}
VITE_COGNITO_CLIENT_ID=${CLIENT_ID}
VITE_APPSYNC_URL=${APPSYNC_URL}
VITE_ENVIRONMENT=${ENV}
VITE_REMOTE_BASE_URL=https://${CLOUDFRONT_DOMAIN}/remotes

# Backend Lambda environment variables
USERS_TABLE=${ENV}-guidedmentor-users
MENTORS_TABLE=${ENV}-guidedmentor-mentors
MENTEES_TABLE=${ENV}-guidedmentor-mentees
SESSIONS_TABLE=${ENV}-guidedmentor-sessions
NOTIFICATIONS_TABLE=${ENV}-guidedmentor-notifications
MEETUPS_TABLE=${ENV}-guidedmentor-meetups
ENGAGEMENT_EVENTS_TABLE=${ENV}-guidedmentor-engagement-events
OPPORTUNITIES_TABLE=${ENV}-guidedmentor-opportunities
EVENT_BUS_NAME=${EVENT_BUS_NAME}
COGNITO_USER_POOL_ID=${USER_POOL_ID}
ENVIRONMENT=${ENV}
EOF
    ;;

  github)
    # GitHub Actions format (for gh CLI)
    echo "Setting GitHub Actions vars for environment: ${ENV}" >&2
    cat <<EOF
gh variable set API_URL --body "${API_GATEWAY_URL}" --env ${ENV}
gh variable set COGNITO_USER_POOL_ID --body "${USER_POOL_ID}" --env ${ENV}
gh variable set COGNITO_CLIENT_ID --body "${CLIENT_ID}" --env ${ENV}
gh variable set APPSYNC_URL --body "${APPSYNC_URL}" --env ${ENV}
gh variable set S3_BUCKET_NAME --body "${SPA_BUCKET}" --env ${ENV}
gh variable set CLOUDFRONT_DISTRIBUTION_ID --body "${CLOUDFRONT_DIST_ID}" --env ${ENV}
EOF
    ;;

  json)
    # JSON format for programmatic consumption
    cat <<EOF
{
  "environment": "${ENV}",
  "frontend": {
    "apiUrl": "${API_GATEWAY_URL}",
    "cognitoUserPoolId": "${USER_POOL_ID}",
    "cognitoClientId": "${CLIENT_ID}",
    "appsyncUrl": "${APPSYNC_URL}",
    "cloudfrontDomain": "${CLOUDFRONT_DOMAIN}",
    "spaBucket": "${SPA_BUCKET}",
    "cloudfrontDistributionId": "${CLOUDFRONT_DIST_ID}"
  },
  "backend": {
    "eventBusName": "${EVENT_BUS_NAME}",
    "tables": {
      "users": "${ENV}-guidedmentor-users",
      "mentors": "${ENV}-guidedmentor-mentors",
      "mentees": "${ENV}-guidedmentor-mentees",
      "sessions": "${ENV}-guidedmentor-sessions",
      "notifications": "${ENV}-guidedmentor-notifications",
      "meetups": "${ENV}-guidedmentor-meetups",
      "engagementEvents": "${ENV}-guidedmentor-engagement-events",
      "opportunities": "${ENV}-guidedmentor-opportunities"
    }
  }
}
EOF
    ;;

  *)
    echo "Unknown format: ${FORMAT}. Use: env, github, or json" >&2
    exit 1
    ;;
esac
