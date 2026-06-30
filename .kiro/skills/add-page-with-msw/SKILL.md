---
name: add-page-with-msw
description: Creates a new page with matching MSW mock handler and realistic simulated data
inclusion: manual
---

# Add Page with MSW

## Steps

1. **Add MSW handler** in `frontend/host-shell/src/mocks/handlers.ts`:
   - `http.get('/v1/{endpoint}', () => HttpResponse.json({...}))`
   - Use realistic Australian names, scores, and session data

2. **Create page** in `frontend/host-shell/src/pages/{PageName}.tsx`:
   - Fetch from the mocked endpoint
   - Skeleton loading state
   - Glass-card styling, score rings, violet/mint accents

3. **Add route** in App.tsx (lazy loaded)

4. **Rebuild**: `npm run build -w host-shell`

5. **Deploy**: `vercel --prod --yes` (from host-shell directory)
