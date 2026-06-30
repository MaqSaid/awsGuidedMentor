# =============================================================================
# Lifecycle Protection — prevent accidental deletion of critical data stores
# =============================================================================
# These resources contain user data that cannot be recreated.
# Apply `lifecycle { prevent_destroy = true }` to:
#
# DynamoDB Tables:
#   - module.identity.aws_dynamodb_table.users
#   - module.mentoring.aws_dynamodb_table.mentors
#   - module.mentoring.aws_dynamodb_table.mentees
#   - module.mentoring.aws_dynamodb_table.sessions
#   - module.engagement.aws_dynamodb_table.notifications
#
# Cognito:
#   - module.identity.aws_cognito_user_pool.main
#
# S3 (Resumes):
#   - module.networking.aws_s3_bucket.resumes
#
# Aurora:
#   - module.analytics.aws_rds_cluster.analytics (staging/prod only)
#
# Note: prevent_destroy must be set on the resource directly in the module.
# This file documents the intent; apply the lifecycle block in each module.
