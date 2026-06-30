---
name: deploy-dev
description: Deploys the full platform to the dev environment (infrastructure, backend, frontend, seed data)
inclusion: manual
---

# Deploy Dev Environment

## Steps

1. **Pre-flight**: `dotnet build GuidedMentor.sln` + `aws sts get-caller-identity`

2. **Infrastructure**: `cd infrastructure && terraform apply -var-file="environments/dev.tfvars"`

3. **Backend Lambdas**:
   ```bash
   for context in Identity Mentoring Content Engagement; do
     dotnet publish "src/${context}/GuidedMentor.${context}.Api/" \
       --configuration Release --runtime linux-x64 -p:PublishAot=true --output "./publish/${context,,}"
     cd "./publish/${context,,}" && zip -r "../../${context,,}-lambda.zip" . && cd ../..
     aws lambda update-function-code --function-name "dev-guidedmentor-${context,,}" --zip-file "fileb://${context,,}-lambda.zip" --publish
   done
   ```

4. **Frontend** (single host-shell build):
   ```bash
   cd frontend && npm run build -w host-shell
   aws s3 sync host-shell/dist/ s3://${SPA_BUCKET}/ --delete
   aws cloudfront create-invalidation --distribution-id ${CF_ID} --paths "/*"
   ```

5. **Seed data**: `dotnet run --project tools/SeedData -- --environment dev`

6. **Verify**: `./scripts/verify-deployment.sh dev`
