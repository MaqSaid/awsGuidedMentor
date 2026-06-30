# =============================================================================
# IAM Access Analyzer
# Identifies overly permissive policies and external access. FREE.
# Continuously monitors IAM roles, S3 policies, KMS grants for unintended access.
# =============================================================================

resource "aws_accessanalyzer_analyzer" "guidedmentor" {
  analyzer_name = "${var.environment}-guidedmentor-analyzer"
  type          = "ACCOUNT"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
    Purpose     = "security-analysis"
  }
}
