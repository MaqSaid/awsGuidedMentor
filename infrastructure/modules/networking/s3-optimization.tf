# =============================================================================
# S3 Intelligent-Tiering Configuration (FREE — actually SAVES money)
# Automatically moves infrequently accessed objects to cheaper storage.
# Resumes uploaded once are rarely re-accessed — perfect for tiering.
#
# Tiers:
#   - Frequent Access: first 30 days (standard pricing)
#   - Infrequent Access: 30-90 days (~40% cheaper)
#   - Archive Instant Access: 90-180 days (~68% cheaper)
#   - Deep Archive: >180 days (~95% cheaper, 12h retrieval)
#
# No retrieval fees (unlike Glacier). Auto-transitions. Zero config after setup.
# =============================================================================

resource "aws_s3_bucket_intelligent_tiering_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  name   = "EntireResumesBucket"

  # Applies to all objects in the bucket
  status = "Enabled"

  tiering {
    access_tier = "ARCHIVE_ACCESS"
    days        = 90
  }

  tiering {
    access_tier = "DEEP_ARCHIVE_ACCESS"
    days        = 180
  }
}

# Also apply to the SPA bucket for old deployment artifacts
resource "aws_s3_bucket_intelligent_tiering_configuration" "spa_assets" {
  bucket = aws_s3_bucket.spa.id
  name   = "OldDeployments"

  # Only applies to objects with the "old-deploy/" prefix
  filter {
    prefix = "old-deploy/"
  }

  status = "Enabled"

  tiering {
    access_tier = "ARCHIVE_ACCESS"
    days        = 30
  }
}
