#!/usr/bin/env pwsh
# GuidedMentor Local Development Startup
# Starts PostgreSQL, runs backend + frontend

Write-Host "=== GuidedMentor Local Dev ===" -ForegroundColor Magenta
Write-Host ""

# Step 1: Start PostgreSQL
Write-Host "[1/3] Starting PostgreSQL..." -ForegroundColor Cyan
docker compose up -d postgres
Start-Sleep -Seconds 5

# Wait for PostgreSQL to be healthy
$retries = 0
while ($retries -lt 10) {
    $health = docker inspect --format='{{.State.Health.Status}}' guidedmentor-postgres 2>$null
    if ($health -eq "healthy") {
        Write-Host "  PostgreSQL is ready" -ForegroundColor Green
        break
    }
    $retries++
    Start-Sleep -Seconds 2
}

if ($retries -ge 10) {
    Write-Host "  WARNING: PostgreSQL health check timed out — continuing anyway" -ForegroundColor Yellow
}

# Step 2: Start backend
Write-Host "[2/3] Starting backend..." -ForegroundColor Cyan
Write-Host ""
Write-Host "  Backend:    http://localhost:5000" -ForegroundColor Green
Write-Host "  Frontend:   http://localhost:3000" -ForegroundColor Green
Write-Host "  API Docs:   http://localhost:5000/scalar/v1" -ForegroundColor Green
Write-Host "  PostgreSQL: localhost:5432" -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C to stop all services." -ForegroundColor Yellow
Write-Host ""

Start-Process -NoNewWindow powershell -ArgumentList "-Command", "dotnet run --project src/Shared/GuidedMentor.LocalDev"

# Step 3: Start frontend
Write-Host "[3/3] Starting frontend..." -ForegroundColor Cyan
Set-Location frontend/host-shell
npm run dev
