# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared library first (for better layer caching)
COPY shared/ shared/

# Copy project files and restore dependencies (for better caching)
COPY test-device/mock-device/MockDevice.csproj test-device/mock-device/
RUN dotnet restore test-device/mock-device/MockDevice.csproj

# Copy application source code and build
COPY test-device/mock-device/ test-device/mock-device/
RUN dotnet build test-device/mock-device/MockDevice.csproj -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish test-device/mock-device/MockDevice.csproj -c Release --no-build --no-restore -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r mockdevice && useradd -r -g mockdevice mockdevice

# Copy published app
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R mockdevice:mockdevice /app
USER mockdevice

# Expose port
EXPOSE 3000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:3000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:3000/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "MockDevice.dll"]