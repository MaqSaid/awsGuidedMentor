# =============================================================================
# Account-level S3 Block Public Access
# Prevents ANY bucket from being made public — defense-in-depth. FREE.
# Even if a bucket policy is misconfigured, public access is blocked.
# =============================================================================

resource "aws_s3_account_public_access_block" "block_all" {
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}
