variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
}

variable "enable_cmk" {
  description = "Enable Customer Managed KMS Keys for encryption"
  type        = bool
  default     = false
}

variable "enable_waf" {
  description = "Enable AWS WAF for API Gateway protection"
  type        = bool
  default     = false
}

variable "enable_alarms" {
  description = "Enable CloudWatch alarms"
  type        = bool
  default     = false
}

variable "kms_key_arn" {
  description = "ARN of the Customer Managed KMS key for encryption (used when enable_cmk is true)"
  type        = string
  default     = ""
}

variable "google_client_id" {
  description = "Google OAuth 2.0 Client ID for social sign-in (empty string disables Google IdP)"
  type        = string
  default     = ""
}

variable "google_client_secret" {
  description = "Google OAuth 2.0 Client Secret for social sign-in"
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
