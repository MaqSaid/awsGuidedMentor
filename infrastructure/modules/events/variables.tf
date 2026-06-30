variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
}

variable "enable_aurora" {
  description = "Enable Aurora-related scheduled jobs (analytics aggregation)"
  type        = bool
  default     = false
}
