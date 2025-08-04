#!/bin/bash

# MeatGeek Infrastructure Deployment Script
# This script deploys the complete MeatGeek infrastructure including all environments

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
LOCATION="northcentralus"
ENVIRONMENTS="prod-only"

# Function to print colored output
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check prerequisites
check_prerequisites() {
    print_message $YELLOW "Checking prerequisites..."
    
    # Check Azure CLI
    if ! command -v az &> /dev/null; then
        print_message $RED "Azure CLI is not installed. Please install it first."
        exit 1
    fi
    
    # Check if logged in to Azure
    if ! az account show &> /dev/null; then
        print_message $RED "Not logged in to Azure. Please run 'az login' first."
        exit 1
    fi
    
    print_message $GREEN "Prerequisites check passed!"
}

# Function to get current user's object ID
get_object_id() {
    local object_id=$(az ad signed-in-user show --query id -o tsv)
    echo $object_id
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -e|--environments)
            ENVIRONMENTS="$2"
            shift 2
            ;;
        -o|--object-id)
            OBJECT_ID="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -l, --location       Azure region (default: northcentralus)"
            echo "  -e, --environments   Environments to deploy: prod-only, prod-staging, prod-staging-test, all (default: prod-only)"
            echo "  -o, --object-id     Object ID for Key Vault access (default: current user)"
            echo "  -h, --help          Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Main deployment
print_message $YELLOW "Starting MeatGeek Infrastructure Deployment"
print_message $YELLOW "=========================================="

# Check prerequisites
check_prerequisites

# Get object ID if not provided
if [ -z "$OBJECT_ID" ]; then
    print_message $YELLOW "Getting current user's object ID..."
    OBJECT_ID=$(get_object_id)
    if [ -z "$OBJECT_ID" ]; then
        print_message $RED "Failed to get object ID. Please provide it using -o flag."
        exit 1
    fi
    print_message $GREEN "Object ID: $OBJECT_ID"
fi

# Show deployment parameters
print_message $YELLOW "\nDeployment Parameters:"
print_message $YELLOW "Location: $LOCATION"
print_message $YELLOW "Environments: $ENVIRONMENTS"
print_message $YELLOW "Object ID: $OBJECT_ID"

# Confirm deployment
read -p "Do you want to proceed with the deployment? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_message $YELLOW "Deployment cancelled."
    exit 0
fi

# Deploy infrastructure
print_message $YELLOW "\nDeploying infrastructure..."
deployment_name="meatgeek-$(date +%Y%m%d-%H%M%S)"

az deployment sub create \
    --name "$deployment_name" \
    --location "$LOCATION" \
    --template-file main.bicep \
    --parameters location="$LOCATION" objectId="$OBJECT_ID" environmentsTodeploy="$ENVIRONMENTS" \
    --output table

if [ $? -eq 0 ]; then
    print_message $GREEN "\nDeployment completed successfully!"
    
    # Get outputs
    print_message $YELLOW "\nDeployment Outputs:"
    az deployment sub show \
        --name "$deployment_name" \
        --query properties.outputs \
        --output table
else
    print_message $RED "\nDeployment failed!"
    exit 1
fi

print_message $YELLOW "\nNext Steps:"
print_message $YELLOW "1. Run ./scripts/populate-secrets.sh to populate Key Vault secrets"
print_message $YELLOW "2. Deploy application code using GitHub Actions"
print_message $YELLOW "3. Configure IoT devices to connect to the IoT Hub"