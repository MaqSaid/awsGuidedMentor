# =============================================================================
# AWS Fault Injection Simulator (FIS) Experiment Templates (HA/DR gap fix #8)
# Used for scheduled chaos testing to validate resilience
# FIS experiments are FREE — you only pay for affected resources during the test
# =============================================================================

variable "environment" {
  description = "Deployment environment"
  type        = string
}

variable "enable_chaos_testing" {
  description = "Enable FIS experiment templates (staging only)"
  type        = bool
  default     = false
}

# Experiment 1: Lambda throttling (simulates account-level concurrency limit)
resource "aws_fis_experiment_template" "lambda_throttle" {
  count = var.enable_chaos_testing ? 1 : 0

  description = "Simulate Lambda throttling to validate circuit breaker and retry behavior"
  role_arn    = aws_iam_role.fis_role[0].arn

  stop_condition {
    source = "none"
  }

  action {
    name      = "throttle-lambda"
    action_id = "aws:lambda:invocation-add-delay"

    parameter {
      key   = "duration"
      value = "PT5M"
    }

    parameter {
      key   = "percentage"
      value = "50"
    }

    parameter {
      key   = "invocationDelay"
      value = "5000"
    }

    target {
      key   = "Functions"
      value = "content-lambda"
    }
  }

  target {
    name           = "content-lambda"
    resource_type  = "aws:lambda:function"
    selection_mode = "ALL"

    resource_tag {
      key   = "BoundedContext"
      value = "Content"
    }
  }

  tags = {
    Environment = var.environment
    Purpose     = "chaos-testing"
  }
}

# Experiment 2: DynamoDB injection (simulates throttling)
resource "aws_fis_experiment_template" "ddb_throttle" {
  count = var.enable_chaos_testing ? 1 : 0

  description = "Simulate DynamoDB throttling to validate retry and degradation behavior"
  role_arn    = aws_iam_role.fis_role[0].arn

  stop_condition {
    source = "none"
  }

  action {
    name      = "pause-ddb"
    action_id = "aws:fis:inject-api-unavailable-error"

    parameter {
      key   = "duration"
      value = "PT3M"
    }

    parameter {
      key   = "service"
      value = "dynamodb"
    }

    parameter {
      key   = "operations"
      value = "GetItem,PutItem,Query"
    }

    parameter {
      key   = "percentage"
      value = "30"
    }

    target {
      key   = "Roles"
      value = "lambda-roles"
    }
  }

  target {
    name           = "lambda-roles"
    resource_type  = "aws:iam:role"
    selection_mode = "ALL"

    resource_tag {
      key   = "Service"
      value = "guidedmentor"
    }
  }

  tags = {
    Environment = var.environment
    Purpose     = "chaos-testing"
  }
}

# =============================================================================
# IAM Role for FIS
# =============================================================================

resource "aws_iam_role" "fis_role" {
  count = var.enable_chaos_testing ? 1 : 0

  name = "${var.environment}-guidedmentor-fis-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = { Service = "fis.amazonaws.com" }
    }]
  })

  tags = {
    Environment = var.environment
    Purpose     = "chaos-testing"
  }
}

resource "aws_iam_role_policy" "fis_policy" {
  count = var.enable_chaos_testing ? 1 : 0

  name = "fis-experiment-policy"
  role = aws_iam_role.fis_role[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "lambda:GetFunction",
          "lambda:InvokeFunction",
          "lambda:UpdateFunctionConfiguration",
          "ec2:DescribeInstances",
          "iam:ListRoles",
          "tag:GetResources"
        ]
        Resource = "*"
      }
    ]
  })
}
