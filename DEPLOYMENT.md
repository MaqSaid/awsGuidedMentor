# GuidedMentor — Production Deployment Guide

Zero-cost production deployment using Cloudflare Pages + Render + Neon PostgreSQL.

## Architecture

```
User → Cloudflare Pages (React SPA, global CDN)
         ↓ API calls (HTTPS)
       Render Web Service (.NET 10 container)
         ├── All bounded context APIs (Identity, Mentoring, Content, Engagement)
         ├── Hangfire background jobs (in-process)
         └── SignalR WebSocket hub
         ↓
       Neon PostgreSQL (serverless, scales to zero)
```

---

## Step 1: Create Neon PostgreSQL Database

1. Go to [neon.tech](https://neon.tech) and sign up (free)
2. Create a new project → name it `guidedmentor`
3. Choose region closest to you (e.g., `us-east-1` or `ap-southeast-2`)
4. Copy the **connection string** — it looks like:
   ```
   postgresql://neondb_owner:password@ep-xyz-123.us-east-2.aws.neon.tech/neondb?sslmode=require
   ```
5. Save this — you'll need it for Render

---

## Step 2: Deploy Backend to Render

1. Go to [render.com](https://render.com) and sign up (free)
2. Click **New** → **Blueprint** → connect your GitHub repo
3. Render auto-detects `render.yaml` and creates the service
4. **Or manually**: New → Web Service → connect repo → select "Docker" environment
5. Set these **Environment Variables** in the Render dashboard:

| Variable | Value |
|----------|-------|
| `ConnectionStrings__DefaultConnection` | Your Neon connection string from Step 1 |
| `Jwt__Secret` | Generate one: `openssl rand -base64 32` (must be 32+ chars) |
| `Jwt__Issuer` | `GuidedMentor` |
| `Jwt__Audience` | `GuidedMentor` |
| `Email__Username` | Your Gmail address |
| `Email__Password` | Your Gmail App Password (not regular password) |
| `Email__FromAddress` | Your Gmail address |
| `CORS__AllowedOrigins` | Your Cloudflare Pages URL (set after Step 3) |

6. Click **Deploy** — first build takes 3-5 minutes
7. Note your Render service URL (e.g., `https://guidedmentor-api.onrender.com`)

### Gmail App Password Setup
1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable 2-Step Verification (required)
3. Go to App Passwords → generate one for "Mail" → "Other (GuidedMentor)"
4. Use this 16-char password as `Email__Password`

---

## Step 3: Deploy Frontend to Cloudflare Pages

1. Go to [pages.cloudflare.com](https://pages.cloudflare.com) and sign up (free)
2. Click **Create a project** → **Connect to Git** → select your repo
3. Configure build settings:

| Setting | Value |
|---------|-------|
| Framework preset | None |
| Build command | `cd frontend/host-shell && npm ci && npm run build` |
| Build output directory | `frontend/host-shell/dist` |
| Root directory | `/` (leave default) |

4. Set **Environment Variables** in Cloudflare Pages settings:

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | Your Render URL from Step 2 (e.g., `https://guidedmentor-api.onrender.com`) |
| `VITE_DISABLE_MOCKS` | `true` |
| `VITE_ENVIRONMENT` | `production` |
| `NODE_VERSION` | `20` |

5. Click **Save and Deploy**
6. Note your Pages URL (e.g., `https://guidedmentor.pages.dev`)

---

## Step 4: Connect Frontend ↔ Backend (CORS)

1. Go back to **Render dashboard** → your service → Environment
2. Set `CORS__AllowedOrigins` to your Cloudflare Pages URL from Step 3
   - Example: `https://guidedmentor.pages.dev`
   - For multiple origins, comma-separate: `https://guidedmentor.pages.dev,https://custom-domain.com`
3. Render will auto-redeploy with the new env var

---

## Step 5: Run Database Migrations

The app runs EF Core migrations on startup automatically. After first deploy:
1. Check Render logs to confirm "Database migrated successfully" or similar
2. If migrations don't run automatically, you can trigger via the health endpoint:
   - Visit `https://guidedmentor-api.onrender.com/v1/health`
   - Should return `{"status":"healthy","environment":"Production"}`

---

## Step 6: Verify Deployment

1. Visit your Cloudflare Pages URL
2. You should see the GuidedMentor landing page
3. Try the magic link login flow (check your email)
4. Browse mentors, view session plans

---

## Troubleshooting

### Backend won't start
- Check Render logs for errors
- Verify `ConnectionStrings__DefaultConnection` is correct
- Ensure Neon database is active (not paused)

### CORS errors in browser console
- Verify `CORS__AllowedOrigins` matches your exact Cloudflare Pages URL (including `https://`)
- No trailing slash

### Cold starts (30-50 seconds)
- Render free tier sleeps after 15 min of inactivity
- First request wakes it up — subsequent requests are fast
- Workaround: set up [UptimeRobot](https://uptimerobot.com) to ping `/v1/health` every 14 min

### Magic link emails not arriving
- Check spam folder
- Verify Gmail App Password is correct
- Ensure 2FA is enabled on your Google account
- Check Render logs for SMTP errors

---

## Cost Summary

| Service | Tier | Cost |
|---------|------|------|
| Cloudflare Pages | Free | $0 |
| Render Web Service | Free (750 hrs/month) | $0 |
| Neon PostgreSQL | Free (0.5 GB) | $0 |
| Gmail SMTP | Free (500 emails/day) | $0 |
| **Total** | | **$0/month** |

---

## Future: Upgrading to Paid

If you outgrow free tiers:
- Render Starter ($7/month): always-on, no cold starts
- Neon Launch ($19/month): 10 GB storage, more compute
- The same codebase works on AWS (Lambda + RDS) with minimal changes — the architecture is platform-agnostic
