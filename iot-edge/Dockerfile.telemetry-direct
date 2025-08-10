# Dockerfile for Telemetry Direct - standalone IoT Hub connection
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project file and restore dependencies
COPY TelemetryDirect.csproj ./
RUN dotnet restore

# Copy source code
COPY TelemetryDirect.cs ./Program.cs

# Build and publish
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out ./

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT ["dotnet", "TelemetryDirect.dll"]