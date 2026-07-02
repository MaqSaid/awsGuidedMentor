---
name: deploy-dev
description: Deploys the full platform to the dev environment (infrastructure, backend, frontend, seed data)
inclusion: manual
---

# Deploy Dev Environment

## Local Development (Free — $0)

### Prerequisites
- Docker Desktop running
- .NET 10 SDK
- Node.js 22+

### Steps

1. **Start PostgreSQL**: `docker compose up -d postgres`
2. **Build backend**: `dotnet build src/Shared/GuidedMentor.LocalDev -c Release`
3. **Run backend**: `dotnet run --project src/Shared/GuidedMentor.LocalDev`
4. **Run frontend**: `cd frontend/host-shell && npm run dev`
5. **Verify**: Open http://localhost:3000

Or use the one-click script: `./scripts/dev-start.ps1`

## Production Deployment (Free — Vercel + Railway + Supabase)

### Prerequisites
- Vercel account (free)
- Railway account (free)
- Supabase account (free)
- Gmail App Password (for magic links)

### Steps

1. **Database**: Create Supabase project → run `scripts/init-db.sql` in SQL editor
2. **Backend**: `railway up` (deploys .NET app from Dockerfile)
3. **Frontend**: `vercel deploy` (deploys React build)
4. **Configure environment variables on Railway**:
   - `ConnectionStrings__DefaultConnection` = Supabase PostgreSQL URL
   - `Email__Username` = Gmail address
   - `Email__Password` = Gmail App Password
   - `FRONTEND_URL` = Vercel URL

### Verify
- Frontend: https://guidedmentor.vercel.app
- Backend: https://guidedmentor.up.railway.app/v1/health
- API Docs: https://guidedmentor.up.railway.app/scalar/v1

## Future: AWS Deployment
Keep `infrastructure/` folder intact. When ready for AWS:
```bash
cd infrastructure && terraform apply -var-file="environments/dev.tfvars"
```
