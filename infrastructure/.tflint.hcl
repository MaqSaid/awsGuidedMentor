# TFLint Configuration for GuidedMentor Infrastructure
# Enforces naming conventions, best practices, and catches deprecated patterns.

config {
  # Enable all available modules
  call_module_type = "all"
}

plugin "terraform" {
  enabled = true
  preset  = "recommended"
}

plugin "aws" {
  enabled = true
  version = "0.32.0"
  source  = "github.com/terraform-linters/tflint-ruleset-aws"
}

# Naming convention rules
rule "terraform_naming_convention" {
  enabled = true
  format  = "snake_case"
}

# Require descriptions on all variables and outputs
rule "terraform_documented_variables" {
  enabled = true
}

rule "terraform_documented_outputs" {
  enabled = true
}

# Disallow deprecated syntax
rule "terraform_deprecated_interpolation" {
  enabled = true
}

# Require type declarations on all variables
rule "terraform_typed_variables" {
  enabled = true
}

# Standard module structure
rule "terraform_standard_module_structure" {
  enabled = true
}
