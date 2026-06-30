# =============================================================================
# SSM Parameter Store — Centralized non-secret configuration. FREE (standard tier).
# Avoids hardcoding values in Lambda environment variables.
# Can be read at runtime by Lambda without Secrets Manager cost.
# =============================================================================

resource "aws_ssm_parameter" "table_prefix" {
  name  = "/${var.environment}/guidedmentor/config/table-prefix"
  type  = "String"
  value = "${var.environment}-guidedmentor"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "bedrock_model_id" {
  name  = "/${var.environment}/guidedmentor/config/bedrock-model-id"
  type  = "String"
  value = "anthropic.claude-sonnet-4-20250514-v1:0"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "rate_limit_api" {
  name        = "/${var.environment}/guidedmentor/config/rate-limit-api"
  type        = "String"
  value       = "100"
  description = "API rate limit: requests per minute per user"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "rate_limit_ai_chat" {
  name        = "/${var.environment}/guidedmentor/config/rate-limit-ai-chat"
  type        = "String"
  value       = "20"
  description = "AI chat rate limit: messages per minute per user"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "session_plan_max_retries" {
  name        = "/${var.environment}/guidedmentor/config/session-plan-max-retries"
  type        = "String"
  value       = "3"
  description = "Maximum retry attempts for AI session plan generation"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "lock_ttl_minutes" {
  name        = "/${var.environment}/guidedmentor/config/lock-ttl-minutes"
  type        = "String"
  value       = "15"
  description = "Mentor lock TTL in minutes"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_ssm_parameter" "notification_max_display" {
  name        = "/${var.environment}/guidedmentor/config/notification-max-display"
  type        = "String"
  value       = "50"
  description = "Maximum notifications to display in the panel"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}
