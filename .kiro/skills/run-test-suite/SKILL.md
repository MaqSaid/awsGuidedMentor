---
name: run-test-suite
description: Runs the appropriate test suite (backend, frontend, property, E2E, or all) and reports results
inclusion: manual
---

# Run Test Suite

## Scopes

- **backend**: `dotnet test GuidedMentor.sln --configuration Release`
- **frontend**: `cd frontend && npm run test`
- **property**: `dotnet test --filter "Category=Property"`
- **e2e**: `cd e2e && npx playwright test`
- **all**: run backend + frontend + property

## Steps

1. Determine scope from user request
2. Execute the appropriate command(s)
3. Report: total passed / failed / skipped
4. If failures: read the failing test file and suggest fix
5. Coverage check (backend): Domain ≥95%, Handlers ≥80%

## Current Test Count
- Backend unit: 412 tests (371 unit + 41 property)
- Frontend unit: 75 tests
- Total: 487 automated tests
