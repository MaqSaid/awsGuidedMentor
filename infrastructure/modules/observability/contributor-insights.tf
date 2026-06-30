# =============================================================================
# CloudWatch Contributor Insights for DynamoDB
# Identifies top partition keys to detect hot partitions before throttling. FREE.
# =============================================================================

resource "aws_cloudwatch_log_group" "contributor_insights" {
  count             = var.enable_alarms ? 1 : 0
  name              = "/aws/dynamodb/contributor-insights/${var.environment}-guidedmentor"
  retention_in_days = 30

  tags = merge(local.common_tags, {
    Name    = "${local.name_prefix}-contributor-insights"
    Purpose = "contributor-insights"
  })
}
