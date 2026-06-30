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

variable "enable_cmk" {
  description = "Enable Customer Managed KMS Keys for Aurora encryption"
  type        = bool
  default     = false
}

variable "kms_key_arn" {
  description = "ARN of the Customer Managed KMS key (required when enable_cmk is true)"
  type        = string
  default     = ""
}

variable "enable_alarms" {
  description = "Enable CloudWatch alarms for Aurora monitoring"
  type        = bool
  default     = false
}

variable "aurora_multi_az" {
  description = "Enable Multi-AZ for Aurora PostgreSQL (adds a reader instance in a second AZ)"
  type        = bool
  default     = false
}

variable "aurora_min_acu" {
  description = "Minimum Aurora Capacity Units (ACU) for Serverless v2 scaling"
  type        = number
  default     = 0.5

  validation {
    condition     = var.aurora_min_acu >= 0.5 && var.aurora_min_acu <= 128
    error_message = "aurora_min_acu must be between 0.5 and 128."
  }
}

variable "aurora_max_acu" {
  description = "Maximum Aurora Capacity Units (ACU) for Serverless v2 scaling"
  type        = number
  default     = 8

  validation {
    condition     = var.aurora_max_acu >= 1 && var.aurora_max_acu <= 128
    error_message = "aurora_max_acu must be between 1 and 128."
  }
}

variable "enable_rds_proxy" {
  description = "Enable RDS Proxy for Lambda connection pooling (staging/prod only)"
  type        = bool
  default     = false
}

variable "tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default     = {}
}
