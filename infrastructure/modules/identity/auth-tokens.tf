# =============================================================================
# DynamoDB AuthTokens Table
# Stores magic link tokens for passwordless authentication.
# Tokens have a 10-minute TTL and are single-use.
# =============================================================================

resource "aws_dynamodb_table" "auth_tokens" {
  name         = "${local.table_prefix}-auth-tokens"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "token"

  attribute {
    name = "token"
    type = "S"
  }

  ttl {
    attribute_name = "expiresAt"
    enabled        = true
  }

  point_in_time_recovery {
    enabled = true
  }

  # Conditional encryption: CMK for staging/prod, AWS-managed for dev
  dynamic "server_side_encryption" {
    for_each = var.enable_cmk ? [1] : []
    content {
      enabled     = true
      kms_key_arn = var.kms_key_arn
    }
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = merge(local.common_tags, {
    Name               = "${local.table_prefix}-auth-tokens"
    DataClassification = "confidential"
  })
}
