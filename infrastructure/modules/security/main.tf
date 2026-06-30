# ==============================================================================
# Security Module
# Manages: KMS CMK, WAF Web ACL, IAM Permission Boundaries, Lambda Code Signing
# Requirements: 21.12, 21.13, 21.14, 21.15
# ==============================================================================

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

locals {
  account_id = data.aws_caller_identity.current.account_id
  region     = data.aws_region.current.name
}

# ==============================================================================
# KMS Customer-Managed Key (staging/prod only)
# Requirement 21.13: CMK for encryption of customer-sensitive data with annual rotation
# ==============================================================================

resource "aws_kms_key" "customer_data" {
  count = var.enable_cmk ? 1 : 0

  description             = "CMK for GuidedMentor customer-sensitive data"
  deletion_window_in_days = 30
  enable_key_rotation     = true

  policy = jsonencode({
    Version = "2012-10-17"
    Id      = "guidedmentor-cmk-policy"
    Statement = [
      {
        Sid    = "AllowAccountRootFullAccess"
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::${local.account_id}:root"
        }
        Action   = "kms:*"
        Resource = "*"
      },
      {
        Sid    = "AllowLambdaRolesEncryptDecrypt"
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::${local.account_id}:root"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:DescribeKey",
          "kms:CreateGrant"
        ]
        Resource = "*"
        Condition = {
          StringLike = {
            "aws:PrincipalArn" = "arn:aws:iam::${local.account_id}:role/${var.environment}-guidedmentor-*-lambda"
          }
        }
      }
    ]
  })

  tags = {
    Name           = "${var.environment}-guidedmentor-customer-data-cmk"
    Environment    = var.environment
    Service        = "security"
    BoundedContext = "platform"
  }
}

resource "aws_kms_alias" "customer_data" {
  count = var.enable_cmk ? 1 : 0

  name          = "alias/${var.environment}-guidedmentor-customer-data"
  target_key_id = aws_kms_key.customer_data[0].key_id
}

# ==============================================================================
# WAF Web ACL (staging/prod only)
# Requirement 21.12: WAF with managed rules, rate limiting, bot control, geo-restriction
# ==============================================================================

resource "aws_wafv2_web_acl" "main" {
  count = var.enable_waf ? 1 : 0

  name        = "${var.environment}-guidedmentor-waf"
  description = "WAF Web ACL for GuidedMentor API Gateway and CloudFront"
  scope       = "REGIONAL"

  default_action {
    allow {}
  }

  # Priority 1: AWS Managed Common Rule Set (SQLi, XSS, known bad inputs)
  rule {
    name     = "aws-managed-common-rule-set"
    priority = 1

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesCommonRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.environment}-guidedmentor-common-rules"
      sampled_requests_enabled   = true
    }
  }

  # Priority 2: Rate-based rule (2000 requests per 5 minutes per IP → block)
  rule {
    name     = "rate-limit-per-ip"
    priority = 2

    action {
      block {}
    }

    statement {
      rate_based_statement {
        limit              = 2000
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.environment}-guidedmentor-rate-limit"
      sampled_requests_enabled   = true
    }
  }

  # Priority 3: AWS Managed Bot Control rule set
  rule {
    name     = "aws-managed-bot-control"
    priority = 3

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesBotControlRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.environment}-guidedmentor-bot-control"
      sampled_requests_enabled   = true
    }
  }

  # Priority 4: Geographic restriction (block all except AU)
  rule {
    name     = "geo-restrict-australia-only"
    priority = 4

    action {
      block {}
    }

    statement {
      not_statement {
        statement {
          geo_match_statement {
            country_codes = ["AU"]
          }
        }
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.environment}-guidedmentor-geo-restrict"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.environment}-guidedmentor-waf"
    sampled_requests_enabled   = true
  }

  tags = {
    Name           = "${var.environment}-guidedmentor-waf"
    Environment    = var.environment
    Service        = "security"
    BoundedContext = "platform"
  }
}

# ==============================================================================
# IAM Permission Boundary (always created)
# Requirement 21.14: Zero-Trust IAM with permission boundaries on Lambda roles
# ==============================================================================

resource "aws_iam_policy" "lambda_permission_boundary" {
  name        = "${var.environment}-guidedmentor-lambda-boundary"
  description = "Permission boundary for all GuidedMentor Lambda execution roles"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowedServices"
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem",
          "dynamodb:Query",
          "dynamodb:Scan",
          "dynamodb:BatchGetItem",
          "dynamodb:BatchWriteItem",
          "dynamodb:ConditionCheckItem",
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket",
          "bedrock:InvokeModel",
          "bedrock:InvokeModelWithResponseStream",
          "appsync:GraphQL",
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "xray:PutTraceSegments",
          "xray:PutTelemetryRecords",
          "cloudwatch:PutMetricData",
          "secretsmanager:GetSecretValue",
          "events:PutEvents",
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:GenerateDataKey*",
          "kms:DescribeKey"
        ]
        Resource = "*"
      },
      {
        Sid    = "DenyIAMActions"
        Effect = "Deny"
        Action = [
          "iam:*"
        ]
        Resource = "*"
      },
      {
        Sid    = "DenyOrganizationsActions"
        Effect = "Deny"
        Action = [
          "organizations:*"
        ]
        Resource = "*"
      },
      {
        Sid    = "DenyCrossAccountAssumeRole"
        Effect = "Deny"
        Action = [
          "sts:AssumeRole"
        ]
        NotResource = [
          "arn:aws:iam::${local.account_id}:role/*"
        ]
      }
    ]
  })

  tags = {
    Name           = "${var.environment}-guidedmentor-lambda-boundary"
    Environment    = var.environment
    Service        = "security"
    BoundedContext = "platform"
  }
}

# ==============================================================================
# Lambda Code Signing Configuration (staging/prod only)
# Requirement 21.14, 21.15: Lambda code signing to prevent unauthorized deployment
# ==============================================================================

resource "aws_signer_signing_profile" "lambda" {
  count = var.enable_cmk ? 1 : 0

  platform_id = "AWSLambda-SHA384-ECDSA"
  name_prefix = "${var.environment}guidedmentor"

  signature_validity_period {
    value = 135
    type  = "MONTHS"
  }

  tags = {
    Name           = "${var.environment}-guidedmentor-signing-profile"
    Environment    = var.environment
    Service        = "security"
    BoundedContext = "platform"
  }
}

resource "aws_lambda_code_signing_config" "main" {
  count = var.enable_cmk ? 1 : 0

  description = "Code signing config for GuidedMentor Lambda functions"

  allowed_publishers {
    signing_profile_version_arns = [aws_signer_signing_profile.lambda[0].version_arn]
  }

  policies {
    untrusted_artifact_on_deployment = "Enforce"
  }
}

# ==============================================================================
# ECR Image Scanning Configuration (staging/prod only)
# Requirement 21.15: Container image scanning for application control
# Enables enhanced scanning on any ECR repositories used for container-based Lambdas
# ==============================================================================

resource "aws_ecr_registry_scanning_configuration" "main" {
  count = var.enable_cmk ? 1 : 0

  scan_type = "ENHANCED"

  rule {
    scan_frequency = "CONTINUOUS_SCAN"
    repository_filter {
      filter      = "${var.environment}-guidedmentor-*"
      filter_type = "WILDCARD"
    }
  }
}
