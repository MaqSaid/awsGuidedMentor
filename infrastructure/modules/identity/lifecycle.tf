# Protect critical Identity resources from accidental deletion
# These cannot be imported or recreated without data loss.

# Note: lifecycle blocks must be inside the resource block itself.
# If using count or for_each, add prevent_destroy inside those resources.
# This file serves as documentation of protection intent.

# Protected resources in this module:
# - aws_dynamodb_table.users (user accounts, profiles)
# - aws_cognito_user_pool.main (authentication credentials)
