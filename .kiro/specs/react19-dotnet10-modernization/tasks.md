# Implementation Plan: React 19 + .NET 10 Modernization

## Overview

This plan modernizes the GuidedMentor platform by adopting React 19.1 features (ref-as-prop, useActionState, useOptimistic, simplified Context, resource preloading) and .NET 10 / C# 14 language features (field keyword, extension members) across frontend and backend layers. Tasks are structured in dependency waves: independent migrations first, dependent features second, and measurement last.

## Tasks

- [x] 1. Remove forwardRef from Design System Components (Wave 1 — Independent)
  - [x] 1.1 Refactor Button component to accept ref as prop
    - Remove `forwardRef` wrapper from `frontend/packages/design-system/src/components/Button.tsx`
    - Convert to function component with `ref?: React.Ref<HTMLButtonElement>` in props
    - Remove `displayName` assignment
    - Verify existing tests pass unchanged
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [x] 1.2 Refactor Input component to accept ref as prop
    - Remove `forwardRef` wrapper from `frontend/packages/design-system/src/components/Input.tsx`
    - Convert to function component with `ref?: React.Ref<HTMLInputElement>` in props
    - Remove `displayName` assignment
    - Verify existing tests pass unchanged
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [x] 1.3 Refactor Modal component to accept ref as prop
    - Remove `forwardRef` wrapper from `frontend/packages/design-system/src/components/Modal.tsx`
    - Convert to function component with `ref?: React.Ref<HTMLDivElement>` in props
    - Remove `displayName` assignment
    - Verify existing tests pass unchanged
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [x] 1.4 Refactor MentorCard component to accept ref as prop
    - Remove `forwardRef` wrapper from `frontend/remotes/mentoring/src/components/MentorCard.tsx`
    - Convert to function component with `ref?: React.Ref<HTMLDivElement>` in props
    - Remove `displayName` assignment
    - Verify existing tests pass unchanged
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [ ]* 1.5 Write property test for component render equivalence (Property 1)
    - **Property 1: Component render equivalence without ref**
    - Use fast-check to generate arbitrary prop combinations (variant, size, disabled, className)
    - Verify refactored function component produces identical DOM output as snapshot baseline when no ref is provided
    - **Validates: Requirements 1.5**

  - [x] 1.6 Verify TypeScript compilation for host-shell after forwardRef removal
    - Run `tsc --noEmit` on host-shell project to confirm zero type errors
    - Run `tsc --noEmit` on design-system package
    - _Requirements: 1.4, 1.7_

- [x] 2. Adopt C# 14 field Keyword in Domain Entities (Wave 1 — Independent)
  - [x] 2.1 Convert Identity bounded context domain entities to use field keyword
    - Scan `src/Identity/Domain/Entities/` for properties with explicit backing fields
    - Replace `private T _fieldName` + accessor references with `field` keyword
    - Skip auto-properties, computed getters, and multi-use backing fields per AC 4.5
    - Verify existing unit tests pass unchanged
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.6_

  - [x] 2.2 Convert Mentoring bounded context domain entities to use field keyword
    - Scan `src/Mentoring/Domain/Entities/` for properties with explicit backing fields
    - Replace `private T _fieldName` + accessor references with `field` keyword
    - Preserve all setter validation and transformation logic
    - Verify existing unit tests pass unchanged
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.6_

  - [x] 2.3 Convert Content bounded context domain entities to use field keyword
    - Scan `src/Content/Domain/Entities/` for properties with explicit backing fields
    - Replace `private T _fieldName` + accessor references with `field` keyword
    - Preserve all setter validation and transformation logic
    - Verify existing unit tests pass unchanged
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.6_

  - [x] 2.4 Convert Engagement bounded context domain entities to use field keyword
    - Scan `src/Engagement/Domain/Entities/` for properties with explicit backing fields
    - Replace `private T _fieldName` + accessor references with `field` keyword
    - Preserve all setter validation and transformation logic
    - Verify existing unit tests pass unchanged
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.6_

  - [ ]* 2.5 Write property test for setter behavioural equivalence (Property 7)
    - **Property 7: Property setter behavioural equivalence with field keyword**
    - Use FsCheck to generate arbitrary valid/invalid inputs for entity setters
    - Verify field-keyword implementation produces identical results (stored values and exceptions) as baseline
    - Tag: `[Trait("Category", "Property")]`, name: `Property7_SetterBehaviouralEquivalence`
    - **Validates: Requirements 4.2, 4.4**

  - [x] 2.6 Verify compilation across all bounded contexts after field keyword adoption
    - Run `dotnet build` on the solution to confirm zero errors and zero new warnings
    - _Requirements: 4.3_

- [x] 3. Simplify Context Provider Syntax (Wave 1 — Independent)
  - [x] 3.1 Update AuthProvider.tsx to use simplified Context syntax
    - Replace `<AuthContext.Provider value={value}>` with `<AuthContext value={value}>` in `frontend/host-shell/src/providers/AuthProvider.tsx`
    - Verify existing tests pass unchanged
    - _Requirements: 6.1, 6.3, 6.4_

  - [x] 3.2 Update RoleProvider.tsx to use simplified Context syntax
    - Replace `<RoleContext.Provider value={value}>` with `<RoleContext value={value}>` in `frontend/host-shell/src/providers/RoleProvider.tsx`
    - Verify existing tests pass unchanged
    - _Requirements: 6.2, 6.3, 6.4_

  - [x] 3.3 Update test files using Context.Provider pattern
    - Search all test files (`**/*.test.tsx`, `**/*.spec.tsx`) for `<*Context.Provider` usage
    - Replace with simplified `<Context value={...}>` syntax
    - Verify all affected tests still pass
    - _Requirements: 6.6_

  - [x] 3.4 Update federated remote context providers to simplified syntax
    - Search `frontend/remotes/*/src/providers/` for `<*Context.Provider` usage
    - Replace with simplified `<Context value={...}>` syntax
    - Verify compilation and existing tests pass
    - _Requirements: 6.5_

- [x] 4. Preload Federated Remote Resources on Navigation Intent (Wave 1 — Independent)
  - [x] 4.1 Create usePreloadRemote hook
    - Create `frontend/host-shell/src/hooks/usePreloadRemote.ts`
    - Implement 150ms debounce timer with `useRef`
    - Call `ReactDOM.preload(entryUrl, { as: 'script' })` after debounce
    - Call `ReactDOM.preconnect(origin)` only for cross-origin remotes
    - Maintain `Set<string>` for deduplication across the page session
    - Wrap preload/preconnect in try/catch to silently discard errors
    - Return `{ onMouseEnter, onFocus, onMouseLeave, onBlur }` event handlers
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [x] 4.2 Create remote entry URL registry and integrate with NavBar
    - Create route-to-remote mapping constant (e.g., in `frontend/host-shell/src/lib/remote-entries.ts`)
    - Integrate `usePreloadRemote` handlers on `<Link>` and `<MobileNavLink>` elements in NavBar
    - Apply handlers to links routing to `/browse`, `/opportunities`, `/notifications`
    - _Requirements: 7.1, 7.6_

  - [ ]* 4.3 Write property test for preload debounce threshold (Property 9)
    - **Property 9: Preload debounce threshold**
    - Use fast-check to generate hover durations between 0-500ms
    - Verify `ReactDOM.preload()` fires iff duration >= 150ms
    - Use fake timers to simulate hover/leave timing
    - **Validates: Requirements 7.1, 7.6**

  - [ ]* 4.4 Write property test for cross-origin preconnect (Property 10)
    - **Property 10: Cross-origin preconnect**
    - Use fast-check `fc.webUrl()` to generate remote entry URLs
    - Verify `ReactDOM.preconnect()` fires iff origin differs from `window.location.origin`
    - **Validates: Requirements 7.2**

  - [ ]* 4.5 Write property test for silent failure handling (Property 11)
    - **Property 11: Preload silent failure handling**
    - Use fast-check to generate various error types (TypeError, NetworkError, 404)
    - Mock `ReactDOM.preload` to throw; verify no unhandled exceptions propagate
    - Verify application state is unaltered
    - **Validates: Requirements 7.5**

  - [ ]* 4.6 Write property test for preload deduplication (Property 12)
    - **Property 12: Preload deduplication (idempotence)**
    - Use fast-check `fc.integer({ min: 1, max: 10 })` for repeat hover count
    - Verify exactly 1 `ReactDOM.preload()` call per unique URL regardless of trigger count
    - **Validates: Requirements 7.7**

- [x] 5. Checkpoint — Wave 1 complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Adopt useActionState for Form Submissions (Wave 2 — Depends on Req 1)
  - [x] 6.1 Refactor LoginPage to use useActionState
    - Modify `frontend/host-shell/src/pages/LoginPage.tsx`
    - Extract `loginAction` async function with `FormActionState` return type
    - Replace manual `useState` for loading/error/sent with `useActionState`
    - Add client-side email validation (reject if no "@") before network request
    - Disable submit button and show spinner via `isPending`
    - Render "check your inbox" view on success state
    - _Requirements: 2.1, 2.2, 2.4, 2.7, 2.8_

  - [x] 6.2 Add accessible error display to LoginPage form action
    - Ensure error messages render in an element with `role="alert"`
    - Connect error element to form field via `aria-describedby`
    - _Requirements: 2.3_

  - [~] 6.3 Refactor OnboardingWizard final step to use useActionState
    - Modify `frontend/host-shell/src/pages/OnboardingWizard.tsx` (or relevant wizard step component)
    - Extract final step submission into a form action function
    - Return success state with `redirectTo: '/dashboard'` to trigger navigation
    - Preserve all previously entered wizard data on error
    - Replace manual `useState` for submission status
    - _Requirements: 2.5, 2.6, 2.7_

  - [ ]* 6.4 Write property test for form action state machine (Property 2)
    - **Property 2: Form action state machine correctness**
    - Use fast-check `fc.string()` to generate email inputs
    - Verify: no "@" → error state returned without network call; has "@" → valid FormActionState returned
    - **Validates: Requirements 2.1, 2.4**

  - [ ]* 6.5 Write property test for error display accessibility (Property 3)
    - **Property 3: Error display accessibility attributes**
    - Use fast-check to generate arbitrary error message strings
    - Render LoginPage with error state; verify `role="alert"` and `aria-describedby` linkage
    - **Validates: Requirements 2.3**

  - [ ]* 6.6 Write property test for wizard data preservation (Property 4)
    - **Property 4: Wizard form data preservation on submission failure**
    - Use fast-check to generate arbitrary form data (name, careerGoal, categories, targetRole)
    - Simulate action error; verify all form data remains displayed unchanged
    - **Validates: Requirements 2.6**

- [ ] 7. Add Optimistic UI Updates for Session Actions (Wave 2 — Independent of Req 1)
  - [x] 7.1 Implement useOptimistic for session completion in SessionPlan
    - Modify `frontend/host-shell/src/pages/SessionPlan.tsx` (or relevant session component)
    - Add `useOptimistic` hook with `OptimisticAction` type for mark-complete action
    - Apply `addOptimistic` on "Mark My Sessions Complete" click
    - Render affected element at `opacity-50` with `aria-busy="true"` while pending
    - On server success: TanStack Query invalidation removes pending state
    - On failure/timeout (10s via AbortController): automatic revert + error toast in `aria-live="polite"` region
    - _Requirements: 3.1, 3.2, 3.3, 3.6_

  - [~] 7.2 Implement useOptimistic for session booking in SessionPlan
    - Add optimistic insert for book-session action
    - Show booked session immediately in pending-confirmation visual state
    - On failure/timeout: remove optimistically added session + error notification (5-second display)
    - _Requirements: 3.4, 3.5, 3.6_

  - [ ]* 7.3 Write property test for optimistic state revert (Property 5)
    - **Property 5: Optimistic state revert on rejection**
    - Use fast-check to generate prior session statuses and action types
    - Simulate rejection; verify displayed state reverts to prior status exactly
    - Verify error notification appears in `aria-live="polite"` region
    - **Validates: Requirements 3.2, 3.5**

  - [ ]* 7.4 Write property test for pending visual indicators (Property 6)
    - **Property 6: Pending optimistic action visual indicators**
    - Use fast-check to generate sessions with pending actions
    - Verify element has `opacity: 0.5` (or `opacity-50` class) and `aria-busy="true"`
    - **Validates: Requirements 3.3**

- [ ] 8. Convert Static Extension Methods to Extension Members (Wave 2 — Depends on Req 4)
  - [x] 8.1 Convert Mentoring bounded context extension methods to extension members
    - Identify extension method classes in `src/Mentoring/` targeting domain/value-object types
    - Convert to `extension(TargetType instance) { ... }` syntax
    - Skip DI registration extensions on `IServiceCollection`
    - Handle mixed classes per AC 5.4 — migrate only extension methods, keep static helpers
    - Verify existing tests pass unchanged
    - _Requirements: 5.1, 5.2, 5.4, 5.5, 5.6_

  - [x] 8.2 Convert Identity bounded context extension methods to extension members
    - Identify extension method classes in `src/Identity/` targeting domain/value-object types
    - Convert to `extension(TargetType instance) { ... }` syntax
    - Skip middleware and DI extensions
    - Verify existing tests pass unchanged
    - _Requirements: 5.1, 5.2, 5.4, 5.5, 5.6_

  - [~] 8.3 Convert Content bounded context extension methods to extension members
    - Identify extension method classes in `src/Content/` targeting domain/value-object types
    - Convert to `extension(TargetType instance) { ... }` syntax
    - Verify existing tests pass unchanged
    - _Requirements: 5.1, 5.2, 5.4, 5.5, 5.6_

  - [~] 8.4 Convert Engagement bounded context extension methods to extension members
    - Identify extension method classes in `src/Engagement/` targeting domain/value-object types
    - Convert to `extension(TargetType instance) { ... }` syntax
    - Verify existing tests pass unchanged
    - _Requirements: 5.1, 5.2, 5.4, 5.5, 5.6_

  - [ ]* 8.5 Write property test for extension member functional equivalence (Property 8)
    - **Property 8: Extension member functional equivalence**
    - Use FsCheck with custom Arbitrary for domain entities
    - Verify extension member invocations produce identical return values to old static method calls
    - Tag: `[Trait("Category", "Property")]`, name: `Property8_ExtensionMemberEquivalence`
    - **Validates: Requirements 5.1, 5.2**

  - [~] 8.6 Verify compilation after extension member conversion
    - Run `dotnet build` on the solution to confirm zero errors
    - Confirm all call sites remain unchanged (dot-notation preserved)
    - _Requirements: 5.3, 5.5_

- [~] 9. Checkpoint — Wave 2 complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 10. Verify AOT Binary Size Improvement with .NET 10 (Wave 3 — Depends on Req 4+5)
  - [~] 10.1 Create AOT baseline tracking file and build script
    - Create `tools/aot-baselines.json` with baseline schema for each Lambda project
    - Create `tools/measure-aot-size.ps1` (or `.sh`) script that:
      - Runs `dotnet publish -c Release -r linux-x64 --self-contained /p:PublishAot=true` for each Lambda project
      - Measures output binary size in MB (2 decimal places)
      - Compares against baseline; reports delta as signed percentage (1 decimal place)
      - Emits `⚠️ REGRESSION` warning if current > baseline
      - On first run (no baseline): records current size as baseline, skips comparison
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [~] 10.2 Update project TFMs to net10.0 and pin .NET 10 SDK
    - Update `TargetFramework` in Lambda project files to `net10.0`
    - Update `global.json` SDK version to .NET 10
    - Verify solution builds successfully with new TFM
    - _Requirements: 8.5_

  - [ ]* 10.3 Write property test for AOT binary size comparison formula (Property 13)
    - **Property 13: AOT binary size comparison correctness**
    - Use fast-check (or FsCheck) `fc.float({ min: 0.01, max: 100 })` pairs for baseline/current
    - Verify delta = `((current - baseline) / baseline) * 100` rounded to 1 decimal
    - Verify regression warning iff `current > baseline`
    - **Validates: Requirements 8.3, 8.4**

  - [~] 10.4 Run AOT build and record initial baselines
    - Execute the measurement script to produce initial baselines
    - Verify non-zero binary sizes recorded
    - Verify script exits cleanly on first-run (skip comparison mode)
    - _Requirements: 8.1, 8.2, 8.6_

- [~] 11. Final Checkpoint — All requirements complete
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation between dependency waves
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Wave 1 tasks (1, 2, 3, 4) are fully independent and can be parallelized
- Wave 2 tasks (6, 7, 8) depend on Wave 1 completions as noted
- Wave 3 (10) depends on backend changes from Wave 1+2 being complete
- Frontend tests use Vitest + React Testing Library + fast-check
- Backend tests use xUnit + FluentAssertions + FsCheck

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "2.1", "2.2", "2.3", "2.4", "3.1", "3.2", "4.1"] },
    { "id": 1, "tasks": ["1.5", "1.6", "2.5", "2.6", "3.3", "3.4", "4.2"] },
    { "id": 2, "tasks": ["4.3", "4.4", "4.5", "4.6", "6.1", "7.1", "8.1", "8.2", "8.3", "8.4"] },
    { "id": 3, "tasks": ["6.2", "6.3", "6.4", "6.5", "7.2", "7.3", "7.4", "8.5", "8.6"] },
    { "id": 4, "tasks": ["6.6", "10.1", "10.2"] },
    { "id": 5, "tasks": ["10.3", "10.4"] }
  ]
}
```
