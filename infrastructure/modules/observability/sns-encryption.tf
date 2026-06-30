# =============================================================================
# SNS Topic Encryption at Rest & Access Policy
# Alarm notifications may contain resource names — encrypt with AWS-managed key. FREE.
#
# NOTE: To enable encryption at rest, add the following attribute to the
# aws_sns_topic.alarms resource in main.tf:
#   kms_master_key_id = "alias/aws/sns"
#
# This cannot be done in a separate file since the resource is already defined.
# This file adds the SNS topic policy for CloudWatch and Cost Explorer access.
# =============================================================================

resource "aws_sns_topic_policy" "alarms_access" {
  arn = aws_sns_topic.alarms.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudWatchAlarms"
        Effect    = "Allow"
        Principal = { Service = "cloudwatch.amazonaws.com" }
        Action    = "SNS:Publish"
        Resource  = aws_sns_topic.alarms.arn
      },
      {
        Sid       = "AllowCostExplorer"
        Effect    = "Allow"
        Principal = { Service = "costalerts.amazonaws.com" }
        Action    = "SNS:Publish"
        Resource  = aws_sns_topic.alarms.arn
      },
      {
        Sid       = "AllowBudgets"
        Effect    = "Allow"
        Principal = { Service = "budgets.amazonaws.com" }
        Action    = "SNS:Publish"
        Resource  = aws_sns_topic.alarms.arn
      }
    ]
  })
}
