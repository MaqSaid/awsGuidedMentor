# Engagement Context Module
# Manages: Notifications, Meetups, EngagementEvents DynamoDB tables,
#           AppSync GraphQL API for real-time subscriptions

# =============================================================================
# KMS Key (conditional — only in staging/prod)
# =============================================================================

resource "aws_kms_key" "engagement" {
  count = var.enable_cmk ? 1 : 0

  description             = "CMK for Engagement context DynamoDB tables"
  deletion_window_in_days = 14
  enable_key_rotation     = true

  tags = {
    Environment  = var.environment
    Service      = "guidedmentor"
    BoundedContext = "engagement"
  }
}

resource "aws_kms_alias" "engagement" {
  count = var.enable_cmk ? 1 : 0

  name          = "alias/${var.environment}-guidedmentor-engagement"
  target_key_id = aws_kms_key.engagement[0].key_id
}

# =============================================================================
# DynamoDB Notifications Table
# Requirement 26.2: Composite partition key pattern (recipientUserId#YYYY-MM)
# distributes writes across time-based partitions for high-volume recipients.
# =============================================================================

resource "aws_dynamodb_table" "notifications" {
  name         = "${var.environment}-guidedmentor-notifications"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "notificationId"

  attribute {
    name = "notificationId"
    type = "S"
  }

  attribute {
    name = "recipientUserId"
    type = "S"
  }

  attribute {
    name = "createdAt"
    type = "S"
  }

  # Composite partition key: recipientUserId#YYYY-MM
  # Distributes writes across monthly partitions to prevent hot keys
  # for users with high notification volumes (Requirement 26.2)
  attribute {
    name = "recipientMonthKey"
    type = "S"
  }

  # Original GSI retained for unread count queries (scans all months)
  global_secondary_index {
    name            = "GSI-Recipient"
    hash_key        = "recipientUserId"
    range_key       = "createdAt"
    projection_type = "ALL"
  }

  # New GSI with composite partition key for write distribution
  # Query pattern: Get notifications for a user in a specific month
  global_secondary_index {
    name            = "GSI-RecipientMonth"
    hash_key        = "recipientMonthKey"
    range_key       = "createdAt"
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  dynamic "server_side_encryption" {
    for_each = var.enable_cmk ? [1] : []
    content {
      enabled     = true
      kms_key_arn = aws_kms_key.engagement[0].arn
    }
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    Environment    = var.environment
    Service        = "guidedmentor"
    BoundedContext = "engagement"
    Table          = "notifications"
  }
}

# =============================================================================
# DynamoDB Meetups Table
# =============================================================================

resource "aws_dynamodb_table" "meetups" {
  name         = "${var.environment}-guidedmentor-meetups"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "meetupEventId"

  attribute {
    name = "meetupEventId"
    type = "S"
  }

  attribute {
    name = "chapter"
    type = "S"
  }

  attribute {
    name = "eventDate"
    type = "S"
  }

  global_secondary_index {
    name            = "GSI-Chapter"
    hash_key        = "chapter"
    range_key       = "eventDate"
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  tags = {
    Environment    = var.environment
    Service        = "guidedmentor"
    BoundedContext = "engagement"
    Table          = "meetups"
  }
}

# =============================================================================
# DynamoDB EngagementEvents Table
# =============================================================================

resource "aws_dynamodb_table" "engagement_events" {
  name         = "${var.environment}-guidedmentor-engagement-events"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "eventId"

  attribute {
    name = "eventId"
    type = "S"
  }

  attribute {
    name = "userIdHash"
    type = "S"
  }

  attribute {
    name = "timestamp"
    type = "N"
  }

  global_secondary_index {
    name            = "GSI-User"
    hash_key        = "userIdHash"
    range_key       = "timestamp"
    projection_type = "ALL"
  }

  ttl {
    attribute_name = "ttl"
    enabled        = true
  }

  tags = {
    Environment    = var.environment
    Service        = "guidedmentor"
    BoundedContext = "engagement"
    Table          = "engagement-events"
  }
}

# =============================================================================
# AppSync GraphQL API — Real-time Notifications
# =============================================================================

resource "aws_appsync_graphql_api" "notifications" {
  name                = "${var.environment}-guidedmentor-notifications"
  authentication_type = "AMAZON_COGNITO_USER_POOLS"

  user_pool_config {
    user_pool_id   = var.cognito_user_pool_id
    default_action = "ALLOW"
    aws_region     = var.aws_region
  }

  log_config {
    cloudwatch_logs_role_arn = aws_iam_role.appsync_logging.arn
    field_log_level         = "ERROR"
  }

  schema = <<-SCHEMA
    type Notification {
      notificationId: ID!
      recipientUserId: String!
      type: String!
      message: String!
      actionUrl: String
      isRead: Boolean!
      createdAt: String!
    }

    type Query {
      getNotification(notificationId: ID!): Notification
    }

    type Mutation {
      publishNotification(
        notificationId: ID!
        recipientUserId: String!
        type: String!
        message: String!
        actionUrl: String
        isRead: Boolean!
        createdAt: String!
      ): Notification
    }

    type Subscription {
      onNotification(recipientUserId: String!): Notification
        @aws_subscribe(mutations: ["publishNotification"])
    }

    schema {
      query: Query
      mutation: Mutation
      subscription: Subscription
    }
  SCHEMA

  tags = {
    Environment    = var.environment
    Service        = "guidedmentor"
    BoundedContext = "engagement"
  }
}

# =============================================================================
# IAM Role for AppSync CloudWatch Logging
# =============================================================================

data "aws_iam_policy_document" "appsync_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["appsync.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "appsync_logging" {
  name               = "${var.environment}-guidedmentor-appsync-logging"
  assume_role_policy = data.aws_iam_policy_document.appsync_assume_role.json

  tags = {
    Environment    = var.environment
    Service        = "guidedmentor"
    BoundedContext = "engagement"
  }
}

data "aws_iam_policy_document" "appsync_logging" {
  statement {
    effect = "Allow"
    actions = [
      "logs:CreateLogGroup",
      "logs:CreateLogStream",
      "logs:PutLogEvents",
    ]
    resources = ["arn:aws:logs:*:*:*"]
  }
}

resource "aws_iam_role_policy" "appsync_logging" {
  name   = "${var.environment}-guidedmentor-appsync-logging"
  role   = aws_iam_role.appsync_logging.id
  policy = data.aws_iam_policy_document.appsync_logging.json
}
