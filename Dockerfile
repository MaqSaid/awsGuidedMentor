# ==============================================================================
# GuidedMentor Backend — Production Dockerfile
# Uses .NET 10 Preview SDK (matches global.json requirement)
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy build configuration
COPY Directory.Build.props ./
COPY global.json ./

# Copy all source projects (not tests/tools — excluded via .dockerignore)
COPY src/ src/

# Restore dependencies
RUN dotnet restore src/Shared/GuidedMentor.LocalDev/GuidedMentor.LocalDev.csproj

# Publish
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
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GuidedMentor.LocalDev.dll"]
