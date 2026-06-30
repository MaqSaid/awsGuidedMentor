# Mentoring Context Module
# Manages: Mentors, Mentees, Sessions, Opportunities DynamoDB tables
# with GSIs, PITR, TTL, and conditional CMK encryption

# =============================================================================
# KMS Key (conditional — staging/prod only)
# =============================================================================

resource "aws_kms_key" "mentoring" {
  count = var.enable_cmk ? 1 : 0

  description             = "CMK for Mentoring context DynamoDB tables (${var.environment})"
  deletion_window_in_days = 14
  enable_key_rotation     = true

  tags = {
    Context = "Mentoring"
  }
}

resource "aws_kms_alias" "mentoring" {
  count = var.enable_cmk ? 1 : 0

  name          = "alias/${var.environment}-guidedmentor-mentoring"
  target_key_id = aws_kms_key.mentoring[0].key_id
}

# =============================================================================
# Mentors Table
# =============================================================================

resource "aws_dynamodb_table" "mentors" {
  name         = "${var.environment}-guidedmentor-mentors"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "mentorId"

  attribute {
    name = "mentorId"
    type = "S"
  }

  attribute {
    name = "userId"
    type = "S"
  }

  attribute {
    name = "isAvailable"
    type = "S"
  }

  attribute {
    name = "activeMenteeCount"
    type = "N"
  }

  global_secondary_index {
    name            = "GSI-UserId"
    hash_key        = "userId"
    projection_type = "ALL"
  }

  global_secondary_index {
    name            = "GSI-Available"
    hash_key        = "isAvailable"
    range_key       = "activeMenteeCount"
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  server_side_encryption {
    enabled     = true
    kms_key_arn = var.enable_cmk ? aws_kms_key.mentoring[0].arn : null
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    Context            = "Mentoring"
    Table              = "Mentors"
    DataClassification = "confidential"
  }
}

# =============================================================================
# Mentees Table
# =============================================================================

resource "aws_dynamodb_table" "mentees" {
  name         = "${var.environment}-guidedmentor-mentees"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "menteeId"

  attribute {
    name = "menteeId"
    type = "S"
  }

  attribute {
    name = "userId"
    type = "S"
  }

  global_secondary_index {
    name            = "GSI-UserId"
    hash_key        = "userId"
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  server_side_encryption {
    enabled     = true
    kms_key_arn = var.enable_cmk ? aws_kms_key.mentoring[0].arn : null
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    Context            = "Mentoring"
    Table              = "Mentees"
    DataClassification = "confidential"
  }
}

# =============================================================================
# Sessions Table
# =============================================================================

resource "aws_dynamodb_table" "sessions" {
  name         = "${var.environment}-guidedmentor-sessions"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "sessionId"

  attribute {
    name = "sessionId"
    type = "S"
  }

  attribute {
    name = "menteeId"
    type = "S"
  }

  attribute {
    name = "mentorId"
    type = "S"
  }

  attribute {
    name = "createdAt"
    type = "S"
  }

  global_secondary_index {
    name            = "GSI-Mentee"
    hash_key        = "menteeId"
    range_key       = "createdAt"
    projection_type = "ALL"
  }

  global_secondary_index {
    name            = "GSI-Mentor"
    hash_key        = "mentorId"
    range_key       = "createdAt"
    projection_type = "ALL"
  }

  ttl {
    attribute_name = "lockExpiresAt"
    enabled        = true
  }

  point_in_time_recovery {
    enabled = true
  }

  server_side_encryption {
    enabled     = true
    kms_key_arn = var.enable_cmk ? aws_kms_key.mentoring[0].arn : null
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    Context            = "Mentoring"
    Table              = "Sessions"
    DataClassification = "confidential"
  }
}

# =============================================================================
# Opportunities Table
# =============================================================================

resource "aws_dynamodb_table" "opportunities" {
  name         = "${var.environment}-guidedmentor-opportunities"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "postingId"

  attribute {
    name = "postingId"
    type = "S"
  }

  attribute {
    name = "mentorId"
    type = "S"
  }

  attribute {
    name = "status"
    type = "S"
  }

  attribute {
    name = "expiresAt"
    type = "S"
  }

  global_secondary_index {
    name            = "GSI-Mentor"
    hash_key        = "mentorId"
    projection_type = "ALL"
  }

  global_secondary_index {
    name            = "GSI-Status"
    hash_key        = "status"
    range_key       = "expiresAt"
    projection_type = "ALL"
  }

  ttl {
    attribute_name = "expiresAt"
    enabled        = true
  }

  point_in_time_recovery {
    enabled = true
  }

  server_side_encryption {
    enabled     = true
    kms_key_arn = var.enable_cmk ? aws_kms_key.mentoring[0].arn : null
  }

  tags = {
    Context            = "Mentoring"
    Table              = "Opportunities"
    DataClassification = "internal"
  }
}
