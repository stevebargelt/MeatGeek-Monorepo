#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Azure Functions Core Tools is installed
if ! command -v func &> /dev/null; then
    echo -e "${RED}Azure Functions Core Tools not found!${NC}"
    echo "Install with: brew install azure-functions-core-tools@4"
    exit 1
fi

# Check if Azurite is installed
if ! command -v azurite &> /dev/null; then
    echo -e "${YELLOW}Warning: Azurite not found!${NC}"
    echo "Install with: npm install -g azurite"
    echo "Continuing without storage emulator..."
    SKIP_AZURITE=true
fi

# Check for local.settings.json files
if [ ! -f "src/MeatGeek.Sessions.Api/local.settings.json" ]; then
    echo -e "${RED}local.settings.json not found for Sessions API!${NC}"
    echo "Copy from: src/MeatGeek.Sessions.Api/local.settings.json.example"
    echo "And add your CosmosDB connection string"
    exit 1
fi

# Start Azurite in background (if available)
if [ -z "$SKIP_AZURITE" ]; then
    echo -e "${GREEN}Starting Azurite storage emulator...${NC}"
    mkdir -p ./azurite-data
    azurite --silent --location ./azurite-data --debug ./azurite-debug.log &
    AZURITE_PID=$!
    sleep 3
fi

# Start Sessions API
echo -e "${GREEN}Starting Sessions API on port 7071...${NC}"
cd src/MeatGeek.Sessions.Api
func start &
API_PID=$!
cd ../..

# Optional: Start Worker API if local.settings.json exists
if [ -f "src/MeatGeek.Sessions.WorkerApi/local.settings.json" ]; then
    echo -e "${GREEN}Starting Worker API on port 7072...${NC}"
    cd src/MeatGeek.Sessions.WorkerApi
    func start --port 7072 &
    WORKER_PID=$!
    cd ../..
else
    echo -e "${YELLOW}Skipping Worker API (no local.settings.json found)${NC}"
fi

# Function to cleanup on exit
cleanup() {
    echo -e "\n${YELLOW}Stopping services...${NC}"
    if [ ! -z "$AZURITE_PID" ]; then
        kill $AZURITE_PID 2>/dev/null
    fi
    if [ ! -z "$API_PID" ]; then
        kill $API_PID 2>/dev/null
    fi
    if [ ! -z "$WORKER_PID" ]; then
        kill $WORKER_PID 2>/dev/null
    fi
    echo -e "${GREEN}Services stopped.${NC}"
    exit
}

# Set trap to cleanup on script exit
trap cleanup INT TERM

# Display status
echo -e "\n${GREEN}Services running:${NC}"
echo "  Sessions API: http://localhost:7071"
if [ ! -z "$WORKER_PID" ]; then
    echo "  Worker API: http://localhost:7072"
fi
echo -e "\n${YELLOW}Press Ctrl+C to stop all services${NC}\n"

# Test endpoint
sleep 5
echo -e "${GREEN}Testing Sessions API health...${NC}"
curl -s http://localhost:7071/api/smoker/test/sessions > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Sessions API is responding${NC}"
else
    echo -e "${YELLOW}⚠ Sessions API may still be starting...${NC}"
fi

# Keep script running
wait