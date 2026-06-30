---
name: run-security-scan
description: Runs Checkov, tflint, NuGet audit, and npm audit to check for security issues
inclusion: manual
---

# Run Security Scan

## Steps

1. **Terraform lint**: `cd infrastructure && tflint --recursive`
2. **Terraform security**: `checkov -d infrastructure/ --quiet`
3. **NuGet vulnerabilities**: `dotnet list GuidedMentor.sln package --vulnerable --include-transitive`
4. **npm vulnerabilities**: `cd frontend && npm audit --audit-level=high`
5. **Report**: list any findings with severity
