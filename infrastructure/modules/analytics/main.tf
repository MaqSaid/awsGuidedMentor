# ==============================================================================
# Analytics Module
# Manages: Aurora PostgreSQL Serverless v2, RDS Proxy, DynamoDB Streams
#           to Aurora replication pipeline, reporting schema
# Note: This entire module is SKIPPED in dev (count = 0 at root level).
#       Only provisioned in staging/prod via var.enable_aurora.
# ==============================================================================

locals {
  cluster_name = "${var.environment}-guidedmentor-analytics"
  db_name      = "guidedmentor_analytics"

  tags = {
    BoundedContext = "Analytics"
    Module         = "analytics"
  }
}

# ==============================================================================
# Data Sources
# ==============================================================================

data "aws_caller_identity" "current" {}

data "aws_availability_zones" "available" {
  state = "available"
}

# ==============================================================================
# VPC Resources (Private Subnets for Aurora)
# ==============================================================================

resource "aws_vpc" "analytics" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(local.tags, {
    Name = "${var.environment}-guidedmentor-analytics-vpc"
  })
}

resource "aws_subnet" "private" {
  count = 2

  vpc_id            = aws_vpc.analytics.id
  cidr_block        = cidrsubnet(aws_vpc.analytics.cidr_block, 8, count.index)
  availability_zone = data.aws_availability_zones.available.names[count.index]

  map_public_ip_on_launch = false

  tags = merge(local.tags, {
    Name = "${var.environment}-guidedmentor-analytics-private-${count.index}"
    Tier = "Private"
  })
}

resource "aws_db_subnet_group" "aurora" {
  name       = "${local.cluster_name}-subnet-group"
  subnet_ids = aws_subnet.private[*].id

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-subnet-group"
  })
}

# Security Group for Aurora (allow PostgreSQL 5432 from Lambda SG)
resource "aws_security_group" "aurora" {
  name_prefix = "${local.cluster_name}-aurora-"
  vpc_id      = aws_vpc.analytics.id
  description = "Security group for Aurora PostgreSQL analytics cluster"

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-aurora-sg"
  })

  lifecycle {
    create_before_destroy = true
  }
}

# Security Group for Lambda (replication pipeline)
resource "aws_security_group" "lambda" {
  name_prefix = "${local.cluster_name}-lambda-"
  vpc_id      = aws_vpc.analytics.id
  description = "Security group for Lambda functions accessing Aurora"

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-lambda-sg"
  })

  lifecycle {
    create_before_destroy = true
  }
}

# Allow inbound PostgreSQL from Lambda SG
resource "aws_security_group_rule" "aurora_ingress_lambda" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.lambda.id
  security_group_id        = aws_security_group.aurora.id
  description              = "Allow PostgreSQL access from Lambda functions"
}

# Allow outbound from Lambda to Aurora
resource "aws_security_group_rule" "lambda_egress_aurora" {
  type                     = "egress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.aurora.id
  security_group_id        = aws_security_group.lambda.id
  description              = "Allow Lambda egress to Aurora PostgreSQL"
}

# Allow outbound HTTPS from Lambda (for AWS API calls)
resource "aws_security_group_rule" "lambda_egress_https" {
  type              = "egress"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  security_group_id = aws_security_group.lambda.id
  description       = "Allow Lambda egress for AWS API calls"
}

# ==============================================================================
# Secrets Manager — Aurora Master Credentials
# ==============================================================================

resource "random_password" "aurora_master" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "aws_secretsmanager_secret" "aurora_master" {
  name                    = "${var.environment}/guidedmentor/analytics/aurora-master"
  description             = "Aurora PostgreSQL master credentials for analytics database"
  recovery_window_in_days = var.environment == "prod" ? 30 : 7

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-master-credentials"
  })
}

resource "aws_secretsmanager_secret_version" "aurora_master" {
  secret_id = aws_secretsmanager_secret.aurora_master.id

  secret_string = jsonencode({
    username = "guidedmentor_admin"
    password = random_password.aurora_master.result
    dbname   = local.db_name
    engine   = "postgres"
    port     = 5432
  })
}

# ==============================================================================
# Aurora PostgreSQL Serverless v2 Cluster
# ==============================================================================

resource "aws_rds_cluster" "analytics" {
  cluster_identifier = local.cluster_name

  engine         = "aurora-postgresql"
  engine_version = "16.4"
  engine_mode    = "provisioned"

  database_name   = local.db_name
  master_username = "guidedmentor_admin"
  master_password = random_password.aurora_master.result

  # Serverless v2 scaling configuration
  serverlessv2_scaling_configuration {
    min_capacity = var.aurora_min_acu
    max_capacity = var.aurora_max_acu
  }

  # Networking
  db_subnet_group_name   = aws_db_subnet_group.aurora.name
  vpc_security_group_ids = [aws_security_group.aurora.id]

  # Backup and retention
  backup_retention_period   = 35
  preferred_backup_window   = "03:00-04:00"
  preferred_maintenance_window = "sun:04:30-sun:05:30"
  copy_tags_to_snapshot     = true
  skip_final_snapshot       = var.environment != "prod"
  final_snapshot_identifier = var.environment == "prod" ? "${local.cluster_name}-final-snapshot" : null

  # Encryption
  storage_encrypted = true
  kms_key_id        = var.enable_cmk ? var.kms_key_arn : null

  # IAM authentication
  iam_database_authentication_enabled = true

  # Deletion protection (prod only)
  deletion_protection = var.environment == "prod"

  # Enable CloudWatch logs
  enabled_cloudwatch_logs_exports = ["postgresql"]

  tags = merge(local.tags, {
    Name = local.cluster_name
  })

  depends_on = [aws_secretsmanager_secret_version.aurora_master]
}

# ==============================================================================
# Aurora Instance (Serverless v2)
# ==============================================================================

resource "aws_rds_cluster_instance" "writer" {
  identifier         = "${local.cluster_name}-writer"
  cluster_identifier = aws_rds_cluster.analytics.id

  instance_class = "db.serverless"
  engine         = aws_rds_cluster.analytics.engine
  engine_version = aws_rds_cluster.analytics.engine_version

  publicly_accessible = false

  # Performance Insights
  performance_insights_enabled = var.environment != "dev"

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-writer"
    Role = "writer"
  })
}

# Additional reader instance for Multi-AZ (conditional)
resource "aws_rds_cluster_instance" "reader" {
  count = var.aurora_multi_az ? 1 : 0

  identifier         = "${local.cluster_name}-reader"
  cluster_identifier = aws_rds_cluster.analytics.id

  instance_class = "db.serverless"
  engine         = aws_rds_cluster.analytics.engine
  engine_version = aws_rds_cluster.analytics.engine_version

  publicly_accessible = false

  performance_insights_enabled = var.environment != "dev"

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-reader"
    Role = "reader"
  })
}

# ==============================================================================
# RDS Proxy (Conditional — staging/prod only)
# ==============================================================================

# IAM role for RDS Proxy to access Secrets Manager
resource "aws_iam_role" "rds_proxy" {
  count = var.enable_rds_proxy ? 1 : 0

  name = "${local.cluster_name}-rds-proxy-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "rds.amazonaws.com"
        }
      }
    ]
  })

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-rds-proxy-role"
  })
}

resource "aws_iam_role_policy" "rds_proxy_secrets" {
  count = var.enable_rds_proxy ? 1 : 0

  name = "${local.cluster_name}-rds-proxy-secrets-policy"
  role = aws_iam_role.rds_proxy[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret"
        ]
        Resource = [aws_secretsmanager_secret.aurora_master.arn]
      },
      {
        Effect = "Allow"
        Action = [
          "kms:Decrypt"
        ]
        Resource = ["*"]
        Condition = {
          StringEquals = {
            "kms:ViaService" = "secretsmanager.${var.aws_region}.amazonaws.com"
          }
        }
      }
    ]
  })
}

resource "aws_db_proxy" "analytics" {
  count = var.enable_rds_proxy ? 1 : 0

  name          = "${var.environment}-guidedmentor-analytics-proxy"
  engine_family = "POSTGRESQL"
  role_arn      = aws_iam_role.rds_proxy[0].arn

  vpc_subnet_ids         = aws_subnet.private[*].id
  vpc_security_group_ids = [aws_security_group.aurora.id]

  require_tls = true

  idle_client_timeout = 300

  auth {
    auth_scheme = "SECRETS"
    iam_auth    = "REQUIRED"
    secret_arn  = aws_secretsmanager_secret.aurora_master.arn
  }

  tags = merge(local.tags, {
    Name = "${var.environment}-guidedmentor-analytics-proxy"
  })

  depends_on = [aws_rds_cluster_instance.writer]
}

resource "aws_db_proxy_default_target_group" "analytics" {
  count = var.enable_rds_proxy ? 1 : 0

  db_proxy_name = aws_db_proxy.analytics[0].name

  connection_pool_config {
    max_connections_percent      = 80
    max_idle_connections_percent = 20
    connection_borrow_timeout    = 120
  }
}

resource "aws_db_proxy_target" "analytics" {
  count = var.enable_rds_proxy ? 1 : 0

  db_proxy_name         = aws_db_proxy.analytics[0].name
  target_group_name     = aws_db_proxy_default_target_group.analytics[0].name
  db_cluster_identifier = aws_rds_cluster.analytics.id
}

# ==============================================================================
# DynamoDB Streams → Lambda → Aurora Replication Pipeline (Placeholder)
# ==============================================================================

# IAM role for the replication Lambda function
resource "aws_iam_role" "replication_lambda" {
  name = "${local.cluster_name}-replication-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-replication-lambda-role"
  })
}

resource "aws_iam_role_policy" "replication_lambda" {
  name = "${local.cluster_name}-replication-lambda-policy"
  role = aws_iam_role.replication_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "DynamoDBStreamsAccess"
        Effect = "Allow"
        Action = [
          "dynamodb:GetRecords",
          "dynamodb:GetShardIterator",
          "dynamodb:DescribeStream",
          "dynamodb:ListStreams"
        ]
        Resource = [
          "arn:aws:dynamodb:${var.aws_region}:${data.aws_caller_identity.current.account_id}:table/${var.environment}-*-guidedmentor-*/stream/*"
        ]
      },
      {
        Sid    = "SecretsManagerAccess"
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [aws_secretsmanager_secret.aurora_master.arn]
      },
      {
        Sid    = "CloudWatchLogs"
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:*"
      },
      {
        Sid    = "VPCNetworkInterfaces"
        Effect = "Allow"
        Action = [
          "ec2:CreateNetworkInterface",
          "ec2:DescribeNetworkInterfaces",
          "ec2:DeleteNetworkInterface"
        ]
        Resource = "*"
      },
      {
        Sid    = "RDSIAMAuth"
        Effect = "Allow"
        Action = [
          "rds-db:connect"
        ]
        Resource = "arn:aws:rds-db:${var.aws_region}:${data.aws_caller_identity.current.account_id}:dbuser:${aws_rds_cluster.analytics.cluster_resource_id}/guidedmentor_admin"
      }
    ]
  })
}

# Placeholder Lambda function for DynamoDB Streams → Aurora replication
# The actual function code will be deployed separately via CI/CD
resource "aws_lambda_function" "ddb_to_aurora_replication" {
  function_name = "${local.cluster_name}-ddb-replication"
  description   = "Replicates DynamoDB Streams events to Aurora PostgreSQL analytics database"

  runtime       = "dotnet8"
  handler       = "GuidedMentor.Analytics.Replication::GuidedMentor.Analytics.Replication.Function::FunctionHandler"
  role          = aws_iam_role.replication_lambda.arn
  timeout       = 60
  memory_size   = 512

  # Placeholder: actual code deployed via CI/CD
  filename         = data.archive_file.lambda_placeholder.output_path
  source_code_hash = data.archive_file.lambda_placeholder.output_base64sha256

  vpc_config {
    subnet_ids         = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.lambda.id]
  }

  environment {
    variables = {
      AURORA_SECRET_ARN  = aws_secretsmanager_secret.aurora_master.arn
      AURORA_ENDPOINT    = aws_rds_cluster.analytics.endpoint
      AURORA_DB_NAME     = local.db_name
      ENVIRONMENT        = var.environment
    }
  }

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-ddb-replication"
  })
}

# Placeholder zip for Lambda (empty handler — real code deployed via CI/CD)
data "archive_file" "lambda_placeholder" {
  type        = "zip"
  output_path = "${path.module}/placeholder_lambda.zip"

  source {
    content  = "placeholder"
    filename = "placeholder.txt"
  }
}

# CloudWatch Log Group for replication Lambda
resource "aws_cloudwatch_log_group" "replication_lambda" {
  name              = "/aws/lambda/${local.cluster_name}-ddb-replication"
  retention_in_days = var.environment == "prod" ? 90 : 14

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-ddb-replication-logs"
  })
}

# SQS Dead Letter Queue for failed replication events
resource "aws_sqs_queue" "replication_dlq" {
  name                      = "${local.cluster_name}-replication-dlq"
  message_retention_seconds = 1209600 # 14 days

  tags = merge(local.tags, {
    Name = "${local.cluster_name}-replication-dlq"
  })
}
