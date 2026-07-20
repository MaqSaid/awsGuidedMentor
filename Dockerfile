# ==============================================================================
# GuidedMentor Backend — Production Dockerfile
# Multi-stage build for .NET 10 (Render deployment)
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution-level files first for layer caching
COPY Directory.Build.props ./
COPY *.sln ./

# Copy all project files for restore
COPY src/Identity/GuidedMentor.Identity.Api/GuidedMentor.Identity.Api.csproj src/Identity/GuidedMentor.Identity.Api/
COPY src/Identity/GuidedMentor.Identity.Application/GuidedMentor.Identity.Application.csproj src/Identity/GuidedMentor.Identity.Application/
COPY src/Identity/GuidedMentor.Identity.Domain/GuidedMentor.Identity.Domain.csproj src/Identity/GuidedMentor.Identity.Domain/
COPY src/Identity/GuidedMentor.Identity.Infrastructure/GuidedMentor.Identity.Infrastructure.csproj src/Identity/GuidedMentor.Identity.Infrastructure/

COPY src/Mentoring/GuidedMentor.Mentoring.Api/GuidedMentor.Mentoring.Api.csproj src/Mentoring/GuidedMentor.Mentoring.Api/
COPY src/Mentoring/GuidedMentor.Mentoring.Application/GuidedMentor.Mentoring.Application.csproj src/Mentoring/GuidedMentor.Mentoring.Application/
COPY src/Mentoring/GuidedMentor.Mentoring.Domain/GuidedMentor.Mentoring.Domain.csproj src/Mentoring/GuidedMentor.Mentoring.Domain/
COPY src/Mentoring/GuidedMentor.Mentoring.Infrastructure/GuidedMentor.Mentoring.Infrastructure.csproj src/Mentoring/GuidedMentor.Mentoring.Infrastructure/

COPY src/Content/GuidedMentor.Content.Api/GuidedMentor.Content.Api.csproj src/Content/GuidedMentor.Content.Api/
COPY src/Content/GuidedMentor.Content.Application/GuidedMentor.Content.Application.csproj src/Content/GuidedMentor.Content.Application/
COPY src/Content/GuidedMentor.Content.Domain/GuidedMentor.Content.Domain.csproj src/Content/GuidedMentor.Content.Domain/
COPY src/Content/GuidedMentor.Content.Infrastructure/GuidedMentor.Content.Infrastructure.csproj src/Content/GuidedMentor.Content.Infrastructure/

COPY src/Engagement/GuidedMentor.Engagement.Api/GuidedMentor.Engagement.Api.csproj src/Engagement/GuidedMentor.Engagement.Api/
COPY src/Engagement/GuidedMentor.Engagement.Application/GuidedMentor.Engagement.Application.csproj src/Engagement/GuidedMentor.Engagement.Application/
COPY src/Engagement/GuidedMentor.Engagement.Domain/GuidedMentor.Engagement.Domain.csproj src/Engagement/GuidedMentor.Engagement.Domain/
COPY src/Engagement/GuidedMentor.Engagement.Infrastructure/GuidedMentor.Engagement.Infrastructure.csproj src/Engagement/GuidedMentor.Engagement.Infrastructure/

COPY src/Shared/GuidedMentor.SharedKernel/GuidedMentor.SharedKernel.csproj src/Shared/GuidedMentor.SharedKernel/
COPY src/Shared/GuidedMentor.SharedInfrastructure/GuidedMentor.SharedInfrastructure.csproj src/Shared/GuidedMentor.SharedInfrastructure/
COPY src/Shared/GuidedMentor.Observability/GuidedMentor.Observability.csproj src/Shared/GuidedMentor.Observability/
COPY src/Shared/GuidedMentor.LocalDev/GuidedMentor.LocalDev.csproj src/Shared/GuidedMentor.LocalDev/

# Restore dependencies (cached layer)
RUN dotnet restore src/Shared/GuidedMentor.LocalDev/GuidedMentor.LocalDev.csproj

# Copy all source code
COPY src/ src/

# Publish the LocalDev project (unified API host)
RUN dotnet publish src/Shared/GuidedMentor.LocalDev/GuidedMentor.LocalDev.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# ==============================================================================
# Runtime stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app

# Install ICU for globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app .

# Render uses PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GuidedMentor.LocalDev.dll"]
