#!/bin/bash

# Wrapper script to start test device from iot-edge root directory
# This handles the path complexity from the reorganization

set -e

echo "========================================="
echo "MeatGeek IoT Test Device Startup"
echo "========================================="

# Check if we're in the right directory
if [ ! -d "test-device" ]; then
    echo "ERROR: This script must be run from the iot-edge/ root directory"
    echo "Current directory: $(pwd)"
    exit 1
fi

# Check if .env exists
if [ ! -f "test-device/.env" ]; then
    echo "Creating test-device/.env from template..."
    cp test-device/.env.azure.example test-device/.env
    echo "Please edit test-device/.env with your Azure IoT Hub connection string"
    echo "Then run this script again"
    exit 1
fi

# Load environment variables
export $(cat test-device/.env | grep -v '^#' | xargs)

# Verify connection string
if [ -z "$DEVICE_CONNECTION_STRING" ]; then
    echo "ERROR: DEVICE_CONNECTION_STRING not set in test-device/.env"
    exit 1
fi

echo ""
echo "Configuration:"
echo "  Device ID: ${DEVICE_ID:-e2e-test-device-1}"
echo "  IoT Hub: ${IOT_HUB_NAME:-testhubmeatgeek}"
echo "  Scenario: ${COOKING_SCENARIO:-brisket}"
echo "  Telemetry Interval: ${TELEMETRY_INTERVAL_SECONDS:-30}s"
echo ""

# Build and start services
echo "Starting services..."
docker-compose -f test-device/deployments/docker-compose.azure.yml up --build -d

echo ""
echo "Services started!"
echo ""
echo "To monitor telemetry in Azure:"
echo "  az iot hub monitor-events --hub-name ${IOT_HUB_NAME:-testhubmeatgeek} --device-id ${DEVICE_ID:-e2e-test-device-1}"
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