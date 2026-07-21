# Requirements Document

## Introduction

This feature modernizes the GuidedMentor platform by adopting React 19.1 and .NET 10 / C# 14 language features across both the frontend and backend. The goal is to reduce boilerplate, improve developer experience, enable optimistic UI patterns, and leverage the latest runtime improvements. The modernization covers 8 priorities: removing forwardRef wrappers, adopting useActionState for form actions, using useOptimistic for instant feedback, adopting the C# 14 `field` keyword, converting static extension methods to extension members, simplifying Context provider syntax, adding resource preloading for federated remotes, and verifying AOT binary size improvements.

## Glossary

- **Design_System**: The shared component library (`@guided-mentor/design-system` and host-shell components) providing Modal, ScoreRing, buttons, and form inputs.
- **Host_Shell**: The main React application shell that loads federated remotes and provides routing, authentication, and layout.
- **Federated_Remote**: A separately bundled micro-frontend loaded at runtime via Module Federation (mentoring, engagement, content remotes).
- **Form_Action**: A React 19 pattern where a form's `action` prop accepts an async function, replacing manual `onSubmit` with `preventDefault`.
- **useActionState**: A React 19 hook that manages pending/error/success state for form actions, returning `[state, formAction, isPending]`.
- **useOptimistic**: A React 19 hook that provides an optimistic value instantly updated in the UI, automatically reverting if the underlying async action fails.
- **field_keyword**: A C# 14 language feature allowing property accessors to reference an auto-generated backing field via the `field` keyword, eliminating explicit `private` backing field declarations.
- **Extension_Member**: A C# 14 feature that replaces `static class` extension method containers with `extension` blocks that can define computed properties, methods, and operators on target types.
- **Context_Provider**: The React component that supplies context values to descendants; React 19 allows `<MyContext value={...}>` directly instead of `<MyContext.Provider value={...}>`.
- **Resource_Preloading**: React DOM APIs (`preload`, `preconnect`) that hint the browser to fetch resources (scripts, stylesheets) before they are needed.
- **AOT_Compilation**: .NET Native Ahead-of-Time compilation that produces self-contained binaries without a JIT compiler, targeting AWS Lambda.
- **Bounded_Context**: An independent domain area (Identity, Mentoring, Content, Engagement) with its own Domain, Application, Infrastructure, and Api layers.
- **Backing_Field**: An explicit private field (e.g., `private int _maxMentees;`) used in a property's get/set accessors for validation or transformation logic.

## Requirements

### Requirement 1: Remove forwardRef from Design System Components

**User Story:** As a frontend developer, I want shared components to accept `ref` as a normal prop, so that I can eliminate the `forwardRef` wrapper boilerplate and simplify component signatures.

#### Acceptance Criteria

1. THE Design_System SHALL accept `ref` as a standard prop on all components that previously used `forwardRef` (Button, Input, and Modal).
2. WHEN a component previously wrapped with `forwardRef` is refactored, THE Design_System SHALL remove the `forwardRef` higher-order function while preserving the same component display name and exported named-export identifier.
3. WHEN a `ref` prop is passed to a refactored component, THE Design_System SHALL attach the ref to the same DOM element type and position in the component tree that the previous `forwardRef` implementation targeted (HTMLButtonElement for Button, HTMLInputElement for Input, HTMLDivElement for Modal).
4. THE Host_Shell SHALL compile with zero TypeScript errors under its existing tsconfig strict-mode settings after removing `forwardRef` wrappers from shared components.
5. WHEN no `ref` prop is provided to a component, THE Design_System SHALL render the component with identical DOM output, applied CSS classes, and event-handler behavior as its pre-refactor version.
6. WHEN a refactored component's TypeScript props type is consumed, THE Design_System SHALL expose `ref` typed to the same element interface previously declared in the `forwardRef` generic (e.g., `React.Ref<HTMLButtonElement>` for Button) without requiring consumers to use a separate `React.ComponentRef` utility type.
7. WHEN existing unit tests for the refactored components are executed, THE Design_System SHALL pass all previously passing tests without modification to test assertions.

### Requirement 2: Adopt useActionState for Form Submissions

**User Story:** As a frontend developer, I want login and onboarding forms to use React 19's `useActionState` hook with form actions, so that pending/error/success states are managed declaratively without manual `useState` orchestration.

#### Acceptance Criteria

1. WHEN the LoginPage form is submitted, THE Host_Shell SHALL invoke the form's action function via `useActionState` initialised with an idle state, and the action function SHALL return a state object representing one of: idle, pending, success, or error.
2. WHEN a form action is pending, THE Host_Shell SHALL set the submit button to disabled and render a visible spinner animation adjacent to the button label, both derived from the `isPending` value returned by `useActionState`.
3. IF a form action returns an error state, THEN THE Host_Shell SHALL display the error description from the returned state object in an element associated with the form field via `aria-describedby` and marked with `role="alert"`.
4. IF the LoginPage form action receives an email value that does not contain an "@" character, THEN THE Host_Shell SHALL return an error state from the action function without making a network request, and display a validation error message to the user.
5. WHEN the OnboardingWizard completes its final step submission, THE Host_Shell SHALL use a form action function via `useActionState` to persist onboarding data, and upon success the action SHALL return a success state that triggers navigation to the dashboard view.
6. IF the OnboardingWizard final step form action fails, THEN THE Host_Shell SHALL return an error state and display the error message to the user while preserving all previously entered form data across wizard steps.
7. THE Host_Shell SHALL not contain manual `useState` declarations for `loading`, `error`, or `sent` states in LoginPage, nor manual `useState` for submission status in OnboardingWizard, where those states are replaced by `useActionState` managed state.
8. WHEN a form action succeeds on LoginPage, THE Host_Shell SHALL transition the UI to display the "check your inbox" confirmation view showing the submitted email address, derived from the success state returned by `useActionState`.

### Requirement 3: Add Optimistic UI Updates for Session Actions

**User Story:** As a mentee, I want to see instant feedback when I mark a session complete or book a session, so that the UI feels responsive without waiting for server confirmation.

#### Acceptance Criteria

1. WHEN a user clicks "Mark My Sessions Complete" on SessionPlan, THE Host_Shell SHALL display the session as completed within 100 milliseconds of the click event, before the server responds, using `useOptimistic`.
2. IF the server rejects the session completion request or fails to respond within 10 seconds, THEN THE Host_Shell SHALL revert the UI to the prior session status within 500 milliseconds of receiving the rejection or timeout, preserving any user input that was not part of the failed action.
3. WHILE an optimistic update is pending server confirmation, THE Host_Shell SHALL render the affected element at 50% opacity and display an accessible status indicator (using `aria-busy="true"`) to communicate that confirmation is in progress.
4. WHEN a session booking action is triggered, THE Host_Shell SHALL optimistically add the booked session to the session list within 100 milliseconds of the user action, before server confirmation, showing the session in a pending-confirmation visual state.
5. IF a session booking request fails or times out after 10 seconds, THEN THE Host_Shell SHALL remove the optimistically added session from the UI within 500 milliseconds and display an error notification visible for at least 5 seconds in an `aria-live="polite"` region, indicating that the booking could not be confirmed.
6. WHEN the server confirms a previously optimistic update, THE Host_Shell SHALL remove the pending-state visual indicator and display the element at full opacity within 200 milliseconds of receiving the server response.

### Requirement 4: Adopt C# 14 field Keyword in Domain Entities

**User Story:** As a backend developer, I want domain entity property accessors to use the C# 14 `field` keyword instead of explicit backing fields, so that boilerplate is reduced while maintaining validation logic.

#### Acceptance Criteria

1. WHEN a domain entity property uses an explicit backing field solely for get/set access (e.g., `private string _title; public string Title { get => _title; set => _title = value; }`), THE Bounded_Context SHALL replace the explicit backing field declaration and references with the `field` keyword in the property accessor body, reducing to a single property declaration with no separate field.
2. THE Bounded_Context SHALL preserve all existing validation logic within property setters after adopting the `field` keyword, such that the setter continues to enforce the same constraints and produces identical rejection behavior for invalid inputs.
3. THE Bounded_Context SHALL compile with zero errors and zero new warnings (beyond any pre-existing baseline) after replacing explicit backing fields with the `field` keyword across all four bounded contexts (Identity, Mentoring, Content, Engagement).
4. WHEN a property accessor contains transformation logic (e.g., `value.Trim()`, `value.ToLowerInvariant()`), THE Bounded_Context SHALL retain that transformation logic and assign the transformed result to `field` as the storage target, producing the same output for identical inputs as the prior implementation.
5. THE Bounded_Context SHALL NOT apply the `field` keyword to properties that meet any of the following exclusion criteria: (a) auto-properties with no explicit backing field, (b) computed-only expression-bodied getters (e.g., `public bool IsActive => !IsDisabled;`), or (c) backing fields used by multiple properties or referenced outside their own property accessor (e.g., a `List<T>` field exposed as `IReadOnlyList<T>`).
6. WHEN the `field` keyword replacement is complete, THE Bounded_Context SHALL pass all existing unit tests in the affected Domain projects without modification to test assertions, confirming behavioral equivalence with the prior backing-field implementation.

### Requirement 5: Convert Static Extension Methods to Extension Members

**User Story:** As a backend developer, I want cross-context adapters and computed utility methods to use C# 14 extension member syntax, so that extension logic reads as natural members on the target type instead of static method calls.

#### Acceptance Criteria

1. WHEN a static extension method class provides computed properties on a domain type, THE Bounded_Context SHALL convert those methods to extension member properties using the C# 14 `extension` syntax.
2. WHEN a static extension method class provides adapter methods for cross-context translation, THE Bounded_Context SHALL convert those methods to extension member methods using the C# 14 `extension` syntax.
3. THE Bounded_Context SHALL compile without errors after converting static extension method classes to extension member blocks.
4. IF a static class contains both extension methods and non-extension static members, THEN THE Bounded_Context SHALL migrate only the extension methods to extension member blocks and retain the remaining static members in the original class.
5. WHEN existing code calls an extension method using dot-notation, THE Bounded_Context SHALL ensure the extension member produces identical call-site syntax and all existing unit tests pass without modification.
6. THE Bounded_Context SHALL NOT convert extension methods whose first parameter is not a domain or value-object type within the same bounded context (e.g., DI registration extensions on `IServiceCollection` and middleware pipeline extensions SHALL remain as traditional static extension methods).

### Requirement 6: Simplify Context Provider Syntax

**User Story:** As a frontend developer, I want context providers to use the React 19 simplified `<Context value={...}>` syntax instead of `<Context.Provider value={...}>`, so that the JSX is cleaner and follows the latest React conventions.

#### Acceptance Criteria

1. THE Host_Shell SHALL replace `<AuthContext.Provider value={value}>...</AuthContext.Provider>` with `<AuthContext value={value}>...</AuthContext>` in AuthProvider.tsx.
2. THE Host_Shell SHALL replace `<RoleContext.Provider value={value}>...</RoleContext.Provider>` with `<RoleContext value={value}>...</RoleContext>` in RoleProvider.tsx.
3. WHEN the simplified context provider syntax is applied, THE Host_Shell SHALL pass all existing unit and integration tests that consume AuthContext or RoleContext values without modification to the consuming components.
4. THE Host_Shell SHALL compile without TypeScript errors after adopting the simplified context provider syntax.
5. WHEN context providers exist in federated remotes or shared packages, THE Federated_Remote SHALL adopt the same simplified `<Context value={...}>` syntax during this modernization pass.
6. THE Host_Shell SHALL update all test files that render context providers using the `<Context.Provider value={...}>` pattern to use the simplified `<Context value={...}>` syntax.

### Requirement 7: Preload Federated Remote Resources on Navigation Intent

**User Story:** As a user navigating the platform, I want federated remote modules to begin loading before I click, so that page transitions feel instantaneous.

#### Acceptance Criteria

1. WHEN a user hovers over or focuses on a navigation link that routes to a federated remote for at least 150 milliseconds, THE Host_Shell SHALL call `ReactDOM.preload()` for the remote's entry script.
2. WHEN a user hovers over or focuses on a navigation link that routes to a federated remote whose origin differs from the host origin, THE Host_Shell SHALL call `ReactDOM.preconnect()` to establish an early connection to that remote origin.
3. THE Host_Shell SHALL preload only the entry-point script of the federated remote, not its full dependency graph.
4. WHEN preloading is triggered, THE Host_Shell SHALL execute the preload asynchronously without blocking the main thread, so that the current page remains interactive and rendering is uninterrupted.
5. IF a preload request fails due to a network error or unavailable remote, THEN THE Host_Shell SHALL discard the failure without displaying an error to the user and without altering application state.
6. THE Host_Shell SHALL use `onMouseEnter` or `onFocus` events on navigation elements as the trigger for resource preloading, and SHALL cancel any pending preload when the corresponding `onMouseLeave` or `onBlur` event fires before the 150-millisecond threshold elapses.
7. THE Host_Shell SHALL deduplicate preload requests so that a remote entry script that has already been preloaded or is currently loading is not requested again during the same page session.

### Requirement 8: Verify AOT Binary Size Improvement with .NET 10

**User Story:** As a platform engineer, I want to verify that rebuilding Lambda functions with .NET 10 AOT produces smaller binaries than the previous build, so that cold-start times and deployment package sizes are tracked.

#### Acceptance Criteria

1. WHEN a .NET 10 AOT build is executed for each Lambda project, THE AOT_Compilation SHALL produce a self-contained binary that is non-zero in size and exits without error when invoked with a health-check argument.
2. WHEN the AOT build completes successfully for a Lambda function, THE AOT_Compilation SHALL record the output binary size in megabytes rounded to two decimal places.
3. THE AOT_Compilation SHALL compare the .NET 10 binary size against the previously recorded baseline size (the last successful AOT build output size stored for the same Lambda function) and report the difference as a signed percentage rounded to one decimal place.
4. IF the .NET 10 AOT binary size exceeds the baseline size by any amount, THEN THE AOT_Compilation SHALL flag the result as a regression by including a visible warning indicator in the build output summary for each affected Lambda function.
5. THE AOT_Compilation SHALL require no application source code changes — only a clean rebuild with the .NET 10 SDK targeting `net10.0` — where project file target-framework updates and SDK version pinning are not considered source code changes.
6. WHEN the build is executed for the first time with no existing baseline for a Lambda function, THE AOT_Compilation SHALL record the current binary size as the initial baseline and skip the comparison step for that function.
