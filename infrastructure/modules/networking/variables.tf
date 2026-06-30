variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
}

variable "enable_waf" {
  description = "Enable AWS WAF for API Gateway and CloudFront protection"
  type        = bool
  default     = false
}

variable "enable_cmk" {
  description = "Enable Customer Managed KMS Keys for S3 encryption (disabled in dev)"
  type        = bool
  default     = false
}

variable "cognito_user_pool_id" {
  description = "Cognito User Pool ID for API Gateway authorizer"
  type        = string
}

variable "cognito_user_pool_arn" {
  description = "Cognito User Pool ARN for API Gateway authorizer"
  type        = string
}

variable "waf_web_acl_arn" {
  description = "WAF Web ACL ARN to associate with API Gateway and CloudFront (created in security module)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default     = {}
}
