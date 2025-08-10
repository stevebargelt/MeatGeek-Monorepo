# Dockerfile for Telemetry Direct - standalone IoT Hub connection
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# Copy shared library first (for better layer caching)
COPY shared/ shared/

# Copy the project file and restore dependencies
COPY test-device/telemetry-direct/TelemetryDirect.csproj test-device/telemetry-direct/
RUN dotnet restore test-device/telemetry-direct/TelemetryDirect.csproj

# Copy source code
COPY test-device/telemetry-direct/ test-device/telemetry-direct/

# Build and publish
RUN dotnet publish test-device/telemetry-direct/TelemetryDirect.csproj -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /src/out ./

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT ["dotnet", "TelemetryDirect.dll"]