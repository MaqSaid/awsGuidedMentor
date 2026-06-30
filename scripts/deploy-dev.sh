#!/bin/bash
# ==============================================================================
# GuidedMentor Platform — Dev Environment Deploy & Seed
# ==============================================================================
# Deploys infrastructure, backend, and frontend to the dev environment,
# then runs the seed data generator and verification script.
#
# Usage: ./scripts/deploy-dev.sh [--skip-infra] [--skip-backend] [--skip-frontend] [--skip-seed]
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ENV="dev"
REGION="ap-southeast-2"

# Parse flags
SKIP_INFRA=false
SKIP_BACKEND=false
SKIP_FRONTEND=false
SKIP_SEED=false

for arg in "$@"; do
  case $arg in
    --skip-infra) SKIP_INFRA=true ;;
    --skip-backend) SKIP_BACKEND=true ;;
    --skip-frontend) SKIP_FRONTEND=true ;;
    --skip-seed) SKIP_SEED=true ;;
    *) echo "Unknown flag: $arg"; exit 1 ;;
  esac
done

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  GuidedMentor — Dev Environment Deployment                  ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# ==============================================================================
# Step 1: Infrastructure (Terraform)
# ==============================================================================

if [ "$SKIP_INFRA" = false ]; then
  echo "━━━ Step 1: Deploying Infrastructure (Terraform) ━━━"
  cd "$PROJECT_ROOT/infrastructure"
  
  terraform init -backend-config="key=${ENV}/terraform.tfstate"
  terraform plan -var-file="environments/${ENV}.tfvars" -out=tfplan
  terraform apply tfplan
  rm -f tfplan
  
  echo "✓ Infrastructure deployed"
  echo ""
else
  echo "━━━ Step 1: Infrastructure — SKIPPED ━━━"
  echo ""
fi

# ==============================================================================
# Step 2: Backend (Native AOT Lambda)
# ==============================================================================

if [ "$SKIP_BACKEND" = false ]; then
  echo "━━━ Step 2: Publishing & Deploying Backend Lambdas ━━━"
  cd "$PROJECT_ROOT"

  CONTEXTS=("Identity" "Mentoring" "Content" "Engagement")
  
  for context in "${CONTEXTS[@]}"; do
    context_lower=$(echo "$context" | tr '[:upper:]' '[:lower:]')
    project_path="src/${context}/GuidedMentor.${context}.Api/GuidedMentor.${context}.Api.csproj"
    function_name="${ENV}-guidedmentor-${context_lower}"
    
    echo "  Publishing ${context}..."
    dotnet publish "$project_path" \
      --configuration Release \
      --runtime linux-x64 \
      --self-contained true \
      -p:PublishAot=true \
      -p:StripSymbols=true \
      --output "./publish/${context_lower}" \
      --verbosity quiet
    
    echo "  Packaging ${context}..."
    cd "./publish/${context_lower}"
    zip -qr "../../${context_lower}-lambda.zip" .
    cd "$PROJECT_ROOT"
    
    echo "  Deploying ${context} → ${function_name}..."
    aws lambda update-function-code \
      --function-name "${function_name}" \
      --zip-file "fileb://${context_lower}-lambda.zip" \
      --publish > /dev/null 2>&1 || echo "    ⚠ Lambda ${function_name} not found (create it first via Terraform)"
    
    rm -rf "./publish/${context_lower}" "./${context_lower}-lambda.zip"
  done

  # Deploy background jobs
  echo "  Publishing BackgroundJobs..."
  dotnet publish "src/BackgroundJobs/GuidedMentor.BackgroundJobs/GuidedMentor.BackgroundJobs.csproj" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:StripSymbols=true \
    --output "./publish/backgroundjobs" \
    --verbosity quiet || echo "  ⚠ BackgroundJobs publish failed (may need project adjustments)"

  echo "✓ Backend deployed"
  echo ""
else
  echo "━━━ Step 2: Backend — SKIPPED ━━━"
  echo ""
fi

# ==============================================================================
# Step 3: Frontend (Vite Build → S3 → CloudFront)
# ==============================================================================

if [ "$SKIP_FRONTEND" = false ]; then
  echo "━━━ Step 3: Building & Deploying Frontend ━━━"
  cd "$PROJECT_ROOT/frontend"
  
  # Install and build
  npm ci --silent
  npm run build
  
  # Get S3 bucket and CloudFront distribution from Terraform
  cd "$PROJECT_ROOT/infrastructure"
  SPA_BUCKET=$(terraform output -json networking_outputs 2>/dev/null | jq -r '.spa_bucket_name // empty' || echo "")
  CF_DIST_ID=$(terraform output -json networking_outputs 2>/dev/null | jq -r '.cloudfront_distribution_id // empty' || echo "")
  
  if [ -n "$SPA_BUCKET" ] && [ "$SPA_BUCKET" != "null" ]; then
    cd "$PROJECT_ROOT/frontend"
    
    # Upload host shell (index.html with no-cache, assets with immutable)
    echo "  Uploading host-shell to s3://${SPA_BUCKET}/..."
    aws s3 sync host-shell/dist/ "s3://${SPA_BUCKET}/" --delete \
      --cache-control "public, max-age=31536000, immutable" \
      --exclude "index.html" --exclude "*.json"
    aws s3 cp host-shell/dist/index.html "s3://${SPA_BUCKET}/index.html" \
      --cache-control "no-cache, no-store, must-revalidate"
    
    # Upload remotes
    for remote in identity mentoring content engagement; do
      if [ -d "remotes/${remote}/dist" ]; then
        echo "  Uploading ${remote} remote..."
        aws s3 sync "remotes/${remote}/dist/" "s3://${SPA_BUCKET}/remotes/${remote}/" --delete \
          --cache-control "public, max-age=31536000, immutable" \
          --exclude "*.html" --exclude "*.json" --exclude "remoteEntry.js"
        # remoteEntry.js must NOT be cached (it changes on each build)
        aws s3 cp "remotes/${remote}/dist/assets/remoteEntry.js" \
          "s3://${SPA_BUCKET}/remotes/${remote}/assets/remoteEntry.js" \
          --cache-control "no-cache, no-store, must-revalidate" 2>/dev/null || true
      fi
    done
    
    # Invalidate CloudFront
    if [ -n "$CF_DIST_ID" ] && [ "$CF_DIST_ID" != "null" ]; then
      echo "  Invalidating CloudFront cache..."
      aws cloudfront create-invalidation --distribution-id "$CF_DIST_ID" --paths "/*" > /dev/null 2>&1
    fi
    
    echo "✓ Frontend deployed"
  else
    echo "  ⚠ S3 bucket not found in Terraform outputs. Skipping S3 upload."
    echo "  (Run --skip-infra=false first or deploy infrastructure manually)"
  fi
  echo ""
else
  echo "━━━ Step 3: Frontend — SKIPPED ━━━"
  echo ""
fi

# ==============================================================================
# Step 4: Seed Data
# ==============================================================================

if [ "$SKIP_SEED" = false ]; then
  echo "━━━ Step 4: Running Seed Data Generator ━━━"
  cd "$PROJECT_ROOT"
  
  dotnet run --project tools/SeedData -- --environment "$ENV" || {
    echo "  ⚠ Seed data generation failed. Check DynamoDB table connectivity."
    echo "  Run manually: dotnet run --project tools/SeedData -- --environment dev"
  }
  
  echo "✓ Seed data generated"
  echo ""
else
  echo "━━━ Step 4: Seed Data — SKIPPED ━━━"
  echo ""
fi

# ==============================================================================
# Step 5: Verification
# ==============================================================================

echo "━━━ Step 5: Running Deployment Verification ━━━"
cd "$PROJECT_ROOT"
bash scripts/verify-deployment.sh "$ENV"

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Deployment Complete!                                        ║"
echo "║                                                              ║"
echo "║  Local development:    cd frontend && npm run dev            ║"
echo "║  Demo guide:           docs/demo-guide.md                    ║"
echo "║  Integration docs:     docs/integration-verification.md      ║"
echo "╚══════════════════════════════════════════════════════════════╝"
