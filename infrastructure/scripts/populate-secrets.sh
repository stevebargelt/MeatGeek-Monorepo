#!/bin/bash

# MeatGeek Key Vault Secret Population Script
# This script populates the Key Vault with required secrets for all services

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to generate a secure secret
generate_secret() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-25
}

# Function to get Key Vault name
get_keyvault_name() {
    local kv_name=$(az keyvault list --query "[?contains(name, 'meatgeek')].name" -o tsv)
    echo $kv_name
}

# Function to set a secret in Key Vault
set_secret() {
    local vault_name=$1
    local secret_name=$2
    local secret_value=$3
    
    print_message $YELLOW "Setting secret: $secret_name"
    az keyvault secret set \
        --vault-name "$vault_name" \
        --name "$secret_name" \
        --value "$secret_value" \
        --output none
}

# Main script
print_message $YELLOW "MeatGeek Key Vault Secret Population"
print_message $YELLOW "===================================="

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    print_message $RED "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Get Key Vault name
print_message $YELLOW "Finding Key Vault..."
KEY_VAULT_NAME=$(get_keyvault_name)

if [ -z "$KEY_VAULT_NAME" ]; then
    print_message $RED "No Key Vault found. Please deploy infrastructure first."
    exit 1
fi

print_message $GREEN "Found Key Vault: $KEY_VAULT_NAME"

# Get resource information
print_message $YELLOW "\nGathering resource information..."

# Get Cosmos DB connection string (already set by Bicep, but verify)
COSMOS_ACCOUNT=$(az cosmosdb list --query "[?contains(name, 'meatgeek')].name" -o tsv)
if [ ! -z "$COSMOS_ACCOUNT" ]; then
    print_message $GREEN "Found Cosmos DB account: $COSMOS_ACCOUNT"
fi

# Get Event Grid Topic information
EVENT_GRID_TOPIC=$(az eventgrid topic list --query "[?contains(name, 'meatgeek')].name" -o tsv)
if [ ! -z "$EVENT_GRID_TOPIC" ]; then
    EVENT_GRID_KEY=$(az eventgrid topic key list --name "$EVENT_GRID_TOPIC" -g MeatGeek-Shared --query key1 -o tsv)
    EVENT_GRID_ENDPOINT=$(az eventgrid topic show --name "$EVENT_GRID_TOPIC" -g MeatGeek-Shared --query endpoint -o tsv)
    print_message $GREEN "Found Event Grid Topic: $EVENT_GRID_TOPIC"
fi

# Get IoT Hub information
IOT_HUB=$(az iot hub list --query "[?contains(name, 'meatgeek')].name" -o tsv)
if [ ! -z "$IOT_HUB" ]; then
    IOT_HUB_CONNECTION=$(az iot hub connection-string show --hub-name "$IOT_HUB" --query connectionString -o tsv)
    IOT_EVENTHUB_ENDPOINT=$(az iot hub show --name "$IOT_HUB" --query properties.eventHubEndpoints.events.endpoint -o tsv)
    print_message $GREEN "Found IoT Hub: $IOT_HUB"
fi

# Get Container Registry information
ACR_NAME=$(az acr list --query "[?contains(name, 'meatgeek')].name" -o tsv)
if [ ! -z "$ACR_NAME" ]; then
    ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query passwords[0].value -o tsv)
    print_message $GREEN "Found Container Registry: $ACR_NAME"
fi

# Get Azure Relay information (if exists)
RELAY_NAMESPACE=$(az relay namespace list --query "[?contains(name, 'meatgeek')].name" -o tsv 2>/dev/null || echo "")
if [ ! -z "$RELAY_NAMESPACE" ]; then
    # Get the first hybrid connection
    HYBRID_CONNECTION=$(az relay hyco list --namespace-name "$RELAY_NAMESPACE" -g MeatGeek-Device --query "[0].name" -o tsv 2>/dev/null || echo "")
    if [ ! -z "$HYBRID_CONNECTION" ]; then
        RELAY_CONNECTION=$(az relay hyco authorization-rule keys list \
            --namespace-name "$RELAY_NAMESPACE" \
            --hybrid-connection-name "$HYBRID_CONNECTION" \
            --name RootManageSharedAccessKey \
            -g MeatGeek-Device \
            --query primaryConnectionString -o tsv 2>/dev/null || echo "")
        print_message $GREEN "Found Relay Namespace: $RELAY_NAMESPACE"
    fi
fi

# Populate secrets
print_message $YELLOW "\nPopulating Key Vault secrets..."

# Environment-agnostic secrets (for backward compatibility)
set_secret "$KEY_VAULT_NAME" "EventGridTopicKey" "${EVENT_GRID_KEY:-$(generate_secret)}"
set_secret "$KEY_VAULT_NAME" "EventGridTopicEndpoint" "${EVENT_GRID_ENDPOINT:-https://placeholder.eventgrid.azure.net}"
set_secret "$KEY_VAULT_NAME" "IoTHubConnectionString" "${IOT_HUB_CONNECTION:-HostName=placeholder.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=$(generate_secret)}"
set_secret "$KEY_VAULT_NAME" "IoTEventHubEndpoint" "${IOT_EVENTHUB_ENDPOINT:-sb://placeholder.servicebus.windows.net}"
set_secret "$KEY_VAULT_NAME" "ContainerRegistryPassword" "${ACR_PASSWORD:-$(generate_secret)}"
set_secret "$KEY_VAULT_NAME" "RelayConnectionString" "${RELAY_CONNECTION:-Endpoint=sb://placeholder.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=$(generate_secret)}"

# Environment-specific secrets
ENVIRONMENTS=("prod" "staging" "test")
for env in "${ENVIRONMENTS[@]}"; do
    # Skip if environment resources don't exist
    if [ "$env" != "prod" ]; then
        RG_EXISTS=$(az group exists -n "MeatGeek-Sessions-$env")
        if [ "$RG_EXISTS" == "false" ]; then
            continue
        fi
    fi
    
    print_message $YELLOW "\nSetting secrets for environment: $env"
    
    # Database name for environment
    DB_NAME=$( [ "$env" == "prod" ] && echo "meatgeek" || echo "meatgeek-$env" )
    
    # Set environment-specific secrets
    set_secret "$KEY_VAULT_NAME" "DatabaseName-$env" "$DB_NAME"
    set_secret "$KEY_VAULT_NAME" "ServicePassword-$env" "$(generate_secret)"
    set_secret "$KEY_VAULT_NAME" "ApiKey-$env" "$(generate_secret)"
    
    # For staging/test, check if environment-specific relay exists
    if [ "$env" != "prod" ] && [ ! -z "$RELAY_NAMESPACE" ]; then
        ENV_RELAY_NS="${RELAY_NAMESPACE}-$env"
        ENV_RELAY_EXISTS=$(az relay namespace list --query "[?name=='$ENV_RELAY_NS'].name" -o tsv 2>/dev/null || echo "")
        if [ ! -z "$ENV_RELAY_EXISTS" ]; then
            ENV_HC=$(az relay hyco list --namespace-name "$ENV_RELAY_NS" -g "MeatGeek-Device-$env" --query "[0].name" -o tsv 2>/dev/null || echo "")
            if [ ! -z "$ENV_HC" ]; then
                ENV_RELAY_CONN=$(az relay hyco authorization-rule keys list \
                    --namespace-name "$ENV_RELAY_NS" \
                    --hybrid-connection-name "$ENV_HC" \
                    --name RootManageSharedAccessKey \
                    -g "MeatGeek-Device-$env" \
                    --query primaryConnectionString -o tsv 2>/dev/null || echo "")
                set_secret "$KEY_VAULT_NAME" "RelayConnectionString-$env" "$ENV_RELAY_CONN"
            fi
        fi
    fi
done

# Application-specific secrets
print_message $YELLOW "\nSetting application-specific secrets..."
set_secret "$KEY_VAULT_NAME" "SessionsApiKey" "$(generate_secret)"
set_secret "$KEY_VAULT_NAME" "DeviceApiKey" "$(generate_secret)"
set_secret "$KEY_VAULT_NAME" "IoTFunctionsKey" "$(generate_secret)"
set_secret "$KEY_VAULT_NAME" "AdminPassword" "$(generate_secret)"

print_message $GREEN "\nSecret population completed successfully!"
print_message $YELLOW "\nSecrets have been populated in Key Vault: $KEY_VAULT_NAME"
print_message $YELLOW "Applications will retrieve these secrets using Managed Service Identity (MSI)"