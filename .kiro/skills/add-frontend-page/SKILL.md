---
name: add-frontend-page
description: Scaffolds a new frontend page inline in host-shell with MSW mock data, route, and lazy loading
inclusion: manual
---

# Add Frontend Page

Creates a new page directly in `frontend/host-shell/src/pages/`.

## Input Required
- Page name (e.g., FeedbackPage)
- API endpoint it consumes
- Brief description

## Steps

1. **Create page** at `frontend/host-shell/src/pages/{PageName}.tsx`:
   - Fetch data in `useEffect` from MSW-mocked endpoint
   - Loading state: use `DashboardSkeleton` or `BrowseSkeleton`
   - Error state with retry
   - Use Outfit font for headings, Inter for body
   - Use `glass-card` for cards, score rings for scores
   - `export default` for lazy loading

2. **Add MSW handler** in `frontend/host-shell/src/mocks/handlers.ts`:
   - Add realistic mock data matching the wireframe style
   - Use names: James Okonkwo, Marcus Williams, Dr. Sarah Chen, etc.

3. **Add lazy route** in `frontend/host-shell/src/App.tsx`:
   ```tsx
   const {PageName} = lazy(() => import('./pages/{PageName}'));
   <Route path="/{path}" element={<{PageName} />} />
   ```

4. **Verify**: `npm run build -w host-shell`

## Conventions
- Colors via Tailwind theme: `text-violet`, `bg-mint/20`, `border-border`
- Headings: `style={{ fontFamily: 'Outfit, sans-serif' }}`
- All cards use `glass-card` class
- Score display uses `<ScoreRing score={n} size="md" />`
