#!/bin/bash

# Start Test IoT Device with Mock BBQ Data
# This script starts a simulated BBQ device that sends telemetry to Azure IoT Hub

set -e

echo "========================================="
echo "MeatGeek IoT Test Device Startup"
echo "========================================="

# Check if .env exists
if [ ! -f ../.env ]; then
    echo "Creating .env from .env.azure.example template..."
    cp ../.env.azure.example ../.env
    echo "Please edit ../.env with your Azure IoT Hub connection string"
    echo "Then run this script again"
    exit 1
fi

# Load environment variables
export $(cat ../.env | grep -v '^#' | xargs)

# Verify connection string
if [ -z "$DEVICE_CONNECTION_STRING" ]; then
    echo "ERROR: DEVICE_CONNECTION_STRING not set in .env"
    exit 1
fi

echo ""
echo "Configuration:"
echo "  Device ID: ${DEVICE_ID:-meatgeek3}"
echo "  IoT Hub: ${IOT_HUB_NAME:-testhubmeatgeek}"
echo "  Scenario: ${COOKING_SCENARIO:-brisket}"
echo "  Telemetry Interval: ${TELEMETRY_INTERVAL_SECONDS:-30}s"
echo ""

# Build and start services
echo "Starting services..."
docker-compose -f ../deployments/docker-compose.azure.yml up --build -d

echo ""
echo "Services started!"
echo ""
echo "To monitor telemetry in Azure:"
echo "  az iot hub monitor-events --hub-name ${IOT_HUB_NAME:-testhubmeatgeek} --device-id ${DEVICE_ID:-meatgeek3}"
echo ""
echo "To view logs:"
echo "  docker-compose -f test-device/deployments/docker-compose.azure.yml logs -f"
echo ""
echo "To stop:"
echo "  docker-compose -f test-device/deployments/docker-compose.azure.yml down"
echo ""
echo "Mock Device API available at: http://localhost:3000"
echo "  - Health: http://localhost:3000/health"
echo "  - Status: http://localhost:3000/api/robots/MeatGeekBot/commands/get_status"
echo ""