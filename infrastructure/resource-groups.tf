# =============================================================================
# AWS Resource Groups — Tag-based grouping for all GuidedMentor resources. FREE.
# Provides one-click visibility in AWS Console for all platform resources.
# =============================================================================

resource "aws_resourcegroups_group" "guidedmentor_all" {
  name        = "${var.environment}-guidedmentor-all"
  description = "All GuidedMentor platform resources for ${var.environment} environment"

  resource_query {
    query = jsonencode({
      ResourceTypeFilters = ["AWS::AllSupported"]
      TagFilters = [
        {
          Key    = "Service"
          Values = ["guidedmentor"]
        },
        {
          Key    = "Environment"
          Values = [var.environment]
        }
      ]
    })
  }

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_resourcegroups_group" "guidedmentor_compute" {
  name        = "${var.environment}-guidedmentor-compute"
  description = "GuidedMentor Lambda functions and API Gateway"

  resource_query {
    query = jsonencode({
      ResourceTypeFilters = ["AWS::Lambda::Function", "AWS::ApiGateway::RestApi"]
      TagFilters = [
        {
          Key    = "Service"
          Values = ["guidedmentor"]
        }
      ]
    })
  }

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}

resource "aws_resourcegroups_group" "guidedmentor_data" {
  name        = "${var.environment}-guidedmentor-data"
  description = "GuidedMentor data stores (DynamoDB, S3, Aurora)"

  resource_query {
    query = jsonencode({
      ResourceTypeFilters = ["AWS::DynamoDB::Table", "AWS::S3::Bucket", "AWS::RDS::DBCluster"]
      TagFilters = [
        {
          Key    = "Service"
          Values = ["guidedmentor"]
        }
      ]
    })
  }

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
  }
}
