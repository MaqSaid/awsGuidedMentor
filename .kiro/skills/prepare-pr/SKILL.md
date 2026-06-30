---
name: prepare-pr
description: Prepares a pull request by running checks, generating a branch, committing, and creating the PR with a structured description
inclusion: manual
---

# Prepare Pull Request

Automates the PR preparation workflow: build, test, commit, push, create PR.

## Steps

1. **Run pre-PR checks**:
   ```bash
   dotnet build GuidedMentor.sln --configuration Release
   dotnet test GuidedMentor.sln --configuration Release --no-build
   cd frontend && npm run lint && npm run test
   ```
   - Abort if any step fails

2. **Determine branch name**:
   - Format: `feat/{short-description}` for features
   - Format: `fix/{short-description}` for bugfixes
   - Format: `chore/{short-description}` for maintenance
   - Ask user for description if not obvious from context

3. **Stage and commit**:
   ```bash
   git checkout -b {branch-name}
   git add -A  # or specific files if user prefers
   git commit -m "{type}: {description}"
   ```
   - Commit message format: `feat: add mentee feedback endpoint`
   - Keep under 70 characters

4. **Push to remote**:
   ```bash
   git push -u origin {branch-name}
   ```

5. **Create PR** (requires GitHub CLI):
   ```bash
   gh pr create \
     --title "{commit message}" \
     --body "## Summary\n{changes}\n\n## Testing\n{test results}\n\n## Checklist\n- [x] Build passes\n- [x] Tests pass\n- [x] No lint errors"
   ```

6. **Report**: PR URL and summary of changes

## PR Description Template
```markdown
## Summary
{Brief description of what changed and why}

## Changes
- {file1}: {what changed}
- {file2}: {what changed}

## Testing
- Backend: {X} tests pass
- Frontend: {Y} tests pass
- Manual verification: {description}

## Requirements
- Addresses: {requirement IDs if applicable}
```

## Notes
- Always push to a new branch, never directly to main
- Check for secrets in staged files before committing
- Prefer staging specific files over `git add .`
