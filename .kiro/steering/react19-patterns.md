---
inclusion: fileMatch
fileMatchPattern: "**/*.tsx"
---

# React 19.1 Modernization Patterns

When modifying `.tsx` files for the react19-dotnet10-modernization spec, apply these exact patterns.

## forwardRef Removal

**Before:**
```tsx
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (props, ref) => <button ref={ref} {...props} />
);
Button.displayName = 'Button';
```

**After:**
```tsx
export function Button({ ref, ...props }: ButtonProps & { ref?: React.Ref<HTMLButtonElement> }) {
  return <button ref={ref} {...props} />;
}
```

Rules:
- Remove the `forwardRef` higher-order function wrapper entirely
- Convert to a named function export (preserve the same export name)
- Add `ref?: React.Ref<ElementType>` to the props type via intersection
- Destructure `ref` from props and pass to the same DOM element
- Remove `displayName` assignment — the function name serves as display name
- Element type mapping: Button→HTMLButtonElement, Input→HTMLInputElement, Modal→HTMLDivElement

## useActionState for Forms

**Import:** `import { useActionState } from 'react';` (NOT from 'react-dom')

**Hook signature:**
```tsx
const [state, formAction, isPending] = useActionState(actionFn, { status: 'idle' });
```

**Action function signature:**
```tsx
async function actionFn(prev: FormActionState, formData: FormData): Promise<FormActionState> { ... }
```

**State type:**
```tsx
type FormActionState =
  | { status: 'idle' }
  | { status: 'success'; email?: string; redirectTo?: string }
  | { status: 'error'; message: string; fieldErrors?: Record<string, string> };
```

Rules:
- Replace all manual `useState` for loading/error/success/sent with `useActionState`
- Use `<form action={formAction}>` instead of `onSubmit` with `preventDefault`
- Derive button disabled state from `isPending`
- Client-side validation (email contains '@') returns error state without network call
- Error display: element with `role="alert"` referenced by field's `aria-describedby`

## useOptimistic for Instant Feedback

**Hook signature:**
```tsx
const [optimisticData, addOptimistic] = useOptimistic(serverData, reducerFn);
```

Rules:
- Call `addOptimistic(action)` immediately on user click (before server call)
- Pair with TanStack Query `mutateAsync` for the actual server request
- Visual pending state: `className="opacity-50"` + `aria-busy="true"` on affected element
- On success: TanStack Query invalidation replaces optimistic state with confirmed data
- On failure: React auto-reverts; show error in `aria-live="polite"` region for 5 seconds
- Use AbortController with 10-second timeout for server requests

## Context Provider Syntax

**Before:** `<AuthContext.Provider value={value}>{children}</AuthContext.Provider>`
**After:** `<AuthContext value={value}>{children}</AuthContext>`

Rules:
- Remove `.Provider` from all context provider JSX tags (opening and closing)
- Apply to production code AND test files that render providers
- The context object itself is used directly as the JSX element

## Resource Preloading (usePreloadRemote hook)

**Imports:** `import { preload, preconnect } from 'react-dom';`

Rules:
- Trigger on `onMouseEnter`/`onFocus` with 150ms debounce timer
- Cancel on `onMouseLeave`/`onBlur` before timer elapses
- Call `preload(url, { as: 'script' })` for entry scripts
- Call `preconnect(origin)` only if origin differs from `window.location.origin`
- Deduplicate via module-level `Set<string>` — never preload same URL twice
- Wrap in try/catch — silently discard all errors, never surface to UI
