variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
}

variable "enable_cmk" {
  description = "Enable Customer Managed KMS Keys for encryption (staging/prod only)"
  type        = bool
  default     = false
}

variable "enable_waf" {
  description = "Enable AWS WAF for API Gateway and CloudFront protection (staging/prod only)"
  type        = bool
  default     = false
}
