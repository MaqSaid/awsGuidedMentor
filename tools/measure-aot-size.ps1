#Requires -Version 7.0
<#
.SYNOPSIS
    Measures AOT-compiled binary sizes for Lambda projects and compares against baselines.

.DESCRIPTION
    For each Lambda project defined in aot-baselines.json:
    - Runs `dotnet publish` with AOT configuration
    - Measures the output binary size in MB (2 decimal places)
    - Compares against the stored baseline
    - Reports delta as a signed percentage (1 decimal place)
    - Emits a warning if the current size exceeds the baseline (regression)
    - On first run (no baseline): records current size as baseline, skips comparison

.EXAMPLE
    ./tools/measure-aot-size.ps1
#>

param(
    [string]$BaselineFile = (Join-Path $PSScriptRoot "aot-baselines.json")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Resolve paths relative to repository root
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-PublishedBinarySizeMb {
    param(
        [string]$ProjectPath,
        [string]$FunctionName
    )

    $fullProjectPath = Join-Path $repoRoot $ProjectPath
    if (-not (Test-Path $fullProjectPath)) {
        Write-Warning "Project file not found: $fullProjectPath — skipping $FunctionName"
        return $null
    }

    $publishDir = Join-Path $repoRoot "artifacts" "aot" $FunctionName

    Write-Host "  Publishing $FunctionName with AOT..." -ForegroundColor Cyan

    $publishArgs = @(
        "publish"
        $fullProjectPath
        "-c", "Release"
        "-r", "linux-x64"
        "--self-contained"
        "/p:PublishAot=true"
        "-o", $publishDir
        "--nologo"
        "-v", "quiet"
    )

    $result = & dotnet @publishArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "AOT publish failed for $FunctionName (exit code: $LASTEXITCODE)"
        Write-Warning ($result | Out-String)
        return $null
    }

    # Find the main binary — look for the native executable (no .dll extension on linux-x64)
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $binaryPath = Get-ChildItem -Path $publishDir -Filter "$projectName" -File -ErrorAction SilentlyContinue |
        Select-Object -First 1

    # Fallback: look for .exe (Windows cross-compile) or any single large binary
    if (-not $binaryPath) {
        $binaryPath = Get-ChildItem -Path $publishDir -Filter "$projectName.exe" -File -ErrorAction SilentlyContinue |
            Select-Object -First 1
    }

    # Final fallback: largest file in publish directory (the native binary)
    if (-not $binaryPath) {
        $binaryPath = Get-ChildItem -Path $publishDir -File -ErrorAction SilentlyContinue |
            Sort-Object Length -Descending |
            Select-Object -First 1
    }

    if (-not $binaryPath) {
        Write-Warning "No binary found in publish output for $FunctionName at $publishDir"
        return $null
    }

    $sizeMb = [math]::Round($binaryPath.Length / 1MB, 2)
    return $sizeMb
}

# Load baselines
if (-not (Test-Path $BaselineFile)) {
    Write-Error "Baseline file not found: $BaselineFile"
    exit 1
}

$baselineData = Get-Content $BaselineFile -Raw | ConvertFrom-Json
$baselines = $baselineData.baselines
$modified = $false

Write-Host ""
Write-Host "======================================================================" -ForegroundColor DarkCyan
Write-Host "          AOT Binary Size Measurement                                 " -ForegroundColor DarkCyan
Write-Host "======================================================================" -ForegroundColor DarkCyan
Write-Host ""

# Results collection for summary table
$results = @()

foreach ($entry in $baselines) {
    $functionName = $entry.functionName
    $projectPath = $entry.projectPath
    $baselineMb = $entry.baselineMb
    $recordedAt = $entry.recordedAt

    Write-Host "----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "  Function: $functionName" -ForegroundColor White

    $currentMb = Get-PublishedBinarySizeMb -ProjectPath $projectPath -FunctionName $functionName

    if ($null -eq $currentMb) {
        $results += [PSCustomObject]@{
            FunctionName = $functionName
            BaselineMb   = if ($recordedAt) { $baselineMb } else { "N/A" }
            CurrentMb    = "FAILED"
            Delta        = "N/A"
            Status       = "BUILD FAILED"
        }
        continue
    }

    # First run — no existing baseline
    if (-not $recordedAt -or $baselineMb -eq 0) {
        Write-Host "  First run — recording $currentMb MB as baseline" -ForegroundColor Yellow

        $entry.baselineMb = $currentMb
        $entry.recordedAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        $entry.sdkVersion = (dotnet --version)
        $modified = $true

        $results += [PSCustomObject]@{
            FunctionName = $functionName
            BaselineMb   = "---"
            CurrentMb    = "$currentMb"
            Delta        = "---"
            Status       = "BASELINE SET"
        }
        continue
    }

    # Compare against baseline
    $deltaPct = [math]::Round((($currentMb - $baselineMb) / $baselineMb) * 100, 1)
    $deltaSign = if ($deltaPct -ge 0) { "+" } else { "" }
    $deltaStr = "${deltaSign}${deltaPct}%"

    if ($currentMb -gt $baselineMb) {
        Write-Host "  ⚠️ REGRESSION: $currentMb MB vs baseline $baselineMb MB ($deltaStr)" -ForegroundColor Red
        $status = "⚠️ REGRESSION"
    }
    elseif ($currentMb -lt $baselineMb) {
        Write-Host "  IMPROVEMENT: $currentMb MB vs baseline $baselineMb MB ($deltaStr)" -ForegroundColor Green
        $status = "IMPROVED"
    }
    else {
        Write-Host "  UNCHANGED: $currentMb MB" -ForegroundColor Green
        $status = "UNCHANGED"
    }

    $results += [PSCustomObject]@{
        FunctionName = $functionName
        BaselineMb   = "$baselineMb"
        CurrentMb    = "$currentMb"
        Delta        = $deltaStr
        Status       = $status
    }
}

# Save updated baselines if modified
if ($modified) {
    $baselineData.baselines = $baselines
    $baselineData | ConvertTo-Json -Depth 10 | Set-Content $BaselineFile -Encoding UTF8
    Write-Host ""
    Write-Host "  Baselines updated: $BaselineFile" -ForegroundColor Yellow
}

# Print summary table
Write-Host ""
Write-Host "======================================================================" -ForegroundColor DarkCyan
Write-Host "  SUMMARY" -ForegroundColor White
Write-Host "======================================================================" -ForegroundColor DarkCyan
Write-Host ""

$headerFormat = "  {0,-40} {1,12} {2,12} {3,10} {4}"
Write-Host ($headerFormat -f "Function", "Baseline MB", "Current MB", "Delta", "Status") -ForegroundColor DarkGray
Write-Host ("  " + ("-" * 95)) -ForegroundColor DarkGray

foreach ($r in $results) {
    $color = if ($r.Status -match "REGRESSION") { "Red" }
             elseif ($r.Status -match "IMPROVED") { "Green" }
             elseif ($r.Status -match "FAILED") { "Red" }
             else { "White" }

    Write-Host ($headerFormat -f $r.FunctionName, $r.BaselineMb, $r.CurrentMb, $r.Delta, $r.Status) -ForegroundColor $color
}

Write-Host ""

# Exit with non-zero if any regressions detected
$regressions = @($results | Where-Object { $_.Status -match "REGRESSION" })
if ($regressions.Count -gt 0) {
    Write-Host "  $($regressions.Count) regression(s) detected!" -ForegroundColor Red
    exit 1
}

Write-Host "  All checks passed." -ForegroundColor Green
exit 0
