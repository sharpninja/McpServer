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

ENV PORT=7147 \
    ASPNETCORE_ENVIRONMENT=Production \
    QBrainAi__Port=7147 \
    QBrainAi__DataSource=mcp.db \
    QBrainAi__DataDirectory=/data \
    QBrainAi__RepoRoot=/workspace \
    QBrainAi__TodoFilePath=docs/Project/TODO.yaml \
    QBrainAi__SessionsPath=docs/sessions \
    VectorIndex__IndexPath=/data/vector.idx \
    Embedding__AutoDownload=true

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:7147/health || exit 1

ENTRYPOINT ["dotnet", "QBrainAi.Support.Mcp.dll"]
