# =============================================================================
# S3 MFA Delete Protection for Resume Bucket
# Prevents accidental/malicious permanent deletion of resume objects. FREE.
#
# Versioning is already enabled on the resumes bucket (main.tf).
# Lifecycle rules for noncurrent versions are already configured (main.tf).
#
# This file documents and enhances the existing protection:
#
# 1. MFA Delete must be enabled via CLI (cannot be done via Terraform for safety):
#    aws s3api put-bucket-versioning \
#      --bucket ${environment}-guidedmentor-resumes \
#      --versioning-configuration Status=Enabled,MFADelete=Enabled \
#      --mfa "arn:aws:iam::ACCOUNT:mfa/DEVICE TOTP_CODE"
#
# 2. The existing lifecycle config (main.tf) transitions noncurrent versions
#    to GLACIER after 30 days. Consider adding GLACIER_IR at 7 days for
#    faster restore if needed:
#      noncurrent_version_transition {
#        noncurrent_days = 7
#        storage_class   = "GLACIER_IR"
#      }
#
# 3. Consider adding noncurrent_version_expiration after 90 days to
#    control storage costs for very old versions.
# =============================================================================

# Object Lock configuration — prevents any deletion for the retention period
# Uncomment if regulatory compliance requires immutable backups
# resource "aws_s3_bucket_object_lock_configuration" "resumes_lock" {
#   bucket = aws_s3_bucket.resumes.id
#
#   rule {
#     default_retention {
#       mode = "GOVERNANCE"
#       days = 30
#     }
#   }
# }
