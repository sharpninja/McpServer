# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy build infrastructure
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props NuGet.config global.json ./

# Copy source and templates
COPY src/ src/
COPY templates/ templates/

# Restore (cached layer)
RUN dotnet restore src/QBrainAi.Support.Mcp/QBrainAi.Support.Mcp.csproj

# Publish
RUN dotnet publish src/QBrainAi.Support.Mcp/QBrainAi.Support.Mcp.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create data directory for DB and vector index
RUN mkdir -p /data /workspace

EXPOSE 7147

# Image defaults stay on Mcp__* through 1.x so an operator override of the same
# name replaces them. A canonical environment value still wins that tie.
ENV PORT=7147 \
    ASPNETCORE_ENVIRONMENT=Production \
    Mcp__Port=7147 \
    Mcp__DataSource=mcp.db \
    Mcp__DataDirectory=/data \
    Mcp__RepoRoot=/workspace \
    Mcp__TodoFilePath=docs/Project/TODO.yaml \
    Mcp__SessionsPath=docs/sessions \
    VectorIndex__IndexPath=/data/vector.idx \
    Embedding__AutoDownload=true

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:7147/health || exit 1

ENTRYPOINT ["dotnet", "QBrainAi.Support.Mcp.dll"]
