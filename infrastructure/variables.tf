variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
  default     = "ap-southeast-2"
}

variable "enable_aurora" {
  description = "Enable Aurora PostgreSQL Serverless v2 for analytics (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_waf" {
  description = "Enable AWS WAF for API Gateway protection (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_cmk" {
  description = "Enable Customer Managed KMS Keys for encryption (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_rds_proxy" {
  description = "Enable RDS Proxy for Aurora connection pooling (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_bedrock_guardrails" {
  description = "Enable Amazon Bedrock Guardrails for content filtering (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_alarms" {
  description = "Enable CloudWatch alarms and alerting (disabled in dev)"
  type        = bool
  default     = false
}

variable "enable_s3_replication" {
  description = "Enable S3 cross-region replication for DR (disabled in dev)"
  type        = bool
  default     = false
}

variable "aurora_multi_az" {
  description = "Enable Multi-AZ for Aurora PostgreSQL"
  type        = bool
  default     = false
}

variable "aurora_min_acu" {
  description = "Minimum Aurora Capacity Units (ACU) for Serverless v2"
  type        = number
  default     = 0.5
}

variable "aurora_max_acu" {
  description = "Maximum Aurora Capacity Units (ACU) for Serverless v2"
  type        = number
  default     = 8
}

variable "kms_key_arn" {
  description = "ARN of the Customer Managed KMS key for encryption (used when enable_cmk is true)"
  type        = string
  default     = ""
}

variable "google_client_id" {
  description = "Google OAuth 2.0 Client ID for Cognito social sign-in (empty string disables Google IdP)"
  type        = string
  default     = ""
}

variable "google_client_secret" {
  description = "Google OAuth 2.0 Client Secret for Cognito social sign-in"
  type        = string
  default     = ""
  sensitive   = true
}

variable "callback_urls" {
  description = "List of allowed OAuth callback URLs for the Cognito app client"
  type        = list(string)
  default     = ["http://localhost:3000/auth/callback"]
}

variable "logout_urls" {
  description = "List of allowed OAuth logout URLs for the Cognito app client"
  type        = list(string)
  default     = ["http://localhost:3000"]
}

variable "budget_amount" {
  description = "Monthly AWS budget limit in USD for cost alerts"
  type        = string
  default     = "50"
}

variable "alarm_email" {
  description = "Email address for alarm and budget notifications (empty string disables email subscription)"
  type        = string
  default     = ""
}
