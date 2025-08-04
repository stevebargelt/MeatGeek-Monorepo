#!/bin/bash

# MeatGeek Key Vault Secret Population Script
# This script populates required secrets after infrastructure deployment

set -e

# Configuration
KV_NAME=${KEY_VAULT_NAME:-"meatgeekkv"}
ENVIRONMENTS=${ENVIRONMENTS:-"prod"}
SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID:-""}

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Generate secure random string
generate_secret() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-25
}

# Generate connection string format
generate_connection_string() {
    local service=$1
    echo "Endpoint=sb://${service}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=$(generate_secret)"
}

# Set secret in Key Vault
set_secret() {
    local name=$1
    local value=$2
    local description=$3
    
    log_info "Setting secret: $name"
    
    # Check if secret already exists
    if az keyvault secret show --vault-name "$KV_NAME" --name "$name" &>/dev/null; then
        log_warn "Secret '$name' already exists. Skipping..."
        return
    fi
    
    # Set the secret
    az keyvault secret set \
        --vault-name "$KV_NAME" \
        --name "$name" \
        --value "$value" \
        --description "$description" \
        --output none
    
    log_info "✓ Secret '$name' set successfully"
}

# Populate secrets for an environment
populate_environment_secrets() {
    local env=$1
    local suffix=""
    
    if [ "$env" != "prod" ]; then
        suffix="-$env"
    fi
    
    log_info "Populating secrets for environment: $env"
    
    # Event Grid Topic Key
    set_secret "EventGridTopicKey${suffix}" \
        "$(generate_secret)" \
        "Event Grid Topic Key for $env environment"
    
    # IoT Hub Connection Strings (shared across environments)
    if [ "$env" == "prod" ]; then
        set_secret "IoTServiceConnection" \
            "$(generate_connection_string "meatgeek-iothub")" \
            "IoT Hub Service Connection String"
        
        set_secret "IoTEventHubEndpoint" \
            "Endpoint=sb://meatgeek-eventhub.servicebus.windows.net/;SharedAccessKeyName=service;SharedAccessKey=$(generate_secret)" \
            "IoT Event Hub Endpoint"
        
        set_secret "IoTSharedAccessConnString" \
            "HostName=meatgeek-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=$(generate_secret)" \
            "IoT Hub Shared Access Connection String"
    fi
    
    # Azure Relay Secrets (for Device API)
    set_secret "RelayNamespace${suffix}" \
        "meatgeek-relay${suffix}" \
        "Azure Relay Namespace for $env"
    
    set_secret "RelayConnectionName${suffix}" \
        "device-relay${suffix}" \
        "Azure Relay Connection Name for $env"
    
    set_secret "RelayKeyName${suffix}" \
        "RootManageSharedAccessKey" \
        "Azure Relay Key Name for $env"
    
    set_secret "RelayKey${suffix}" \
        "$(generate_secret)" \
        "Azure Relay Key for $env"
    
    # IoT Service Connections (environment-specific)
    set_secret "InfernoIoTServiceConnection${suffix}" \
        "HostName=inferno-iothub${suffix}.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=$(generate_secret)" \
        "Inferno IoT Service Connection for $env"
    
    set_secret "MeatGeekIoTServiceConnection${suffix}" \
        "HostName=meatgeek-iothub${suffix}.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=$(generate_secret)" \
        "MeatGeek IoT Service Connection for $env"
}

# Retrieve actual values from deployed resources
populate_from_resources() {
    local env=$1
    local suffix=""
    
    if [ "$env" != "prod" ]; then
        suffix="-$env"
    fi
    
    log_info "Retrieving actual values from deployed resources for: $env"
    
    # Get Cosmos DB connection string (already handled by Bicep)
    # Bicep template already stores this as SharedCosmosConnectionString${suffix}
    
    # Get Event Grid Topic Key
    local topic_name="meatgeek-session${suffix}"
    local topic_key=$(az eventgrid topic key list \
        --name "$topic_name" \
        --resource-group "MeatGeek-Shared" \
        --query "key1" -o tsv 2>/dev/null) || true
    
    if [ -n "$topic_key" ]; then
        set_secret "EventGridTopicKey${suffix}" \
            "$topic_key" \
            "Event Grid Topic Key for $env (actual)"
    fi
    
    # Get Storage Account keys for Function Apps
    # This would be done per service, example for sessions:
    local sessions_rg="MeatGeek-Sessions${suffix}"
    local storage_accounts=$(az storage account list \
        --resource-group "$sessions_rg" \
        --query "[].name" -o tsv 2>/dev/null) || true
    
    for storage in $storage_accounts; do
        local key=$(az storage account keys list \
            --account-name "$storage" \
            --resource-group "$sessions_rg" \
            --query "[0].value" -o tsv 2>/dev/null) || true
        
        if [ -n "$key" ]; then
            local conn_string="DefaultEndpointsProtocol=https;AccountName=${storage};AccountKey=${key};EndpointSuffix=core.windows.net"
            set_secret "${storage}-ConnectionString" \
                "$conn_string" \
                "Storage account connection string for $storage"
        fi
    done
}

# Main execution
main() {
    log_info "🔐 MeatGeek Key Vault Secret Population"
    log_info "====================================="
    
    # Check prerequisites
    if ! command -v az &>/dev/null; then
        log_error "Azure CLI is not installed"
        exit 1
    fi
    
    # Login check
    if ! az account show &>/dev/null; then
        log_error "Not logged in to Azure. Run 'az login' first"
        exit 1
    fi
    
    # Parse environments
    IFS=',' read -ra ENV_ARRAY <<< "$ENVIRONMENTS"
    
    # Process each environment
    for env in "${ENV_ARRAY[@]}"; do
        case $env in
            "prod"|"staging"|"test")
                populate_environment_secrets "$env"
                populate_from_resources "$env"
                ;;
            *)
                log_warn "Unknown environment: $env"
                ;;
        esac
    done
    
    log_info "✅ Secret population completed!"
    
    # Display summary
    log_info ""
    log_info "Secrets in Key Vault:"
    az keyvault secret list \
        --vault-name "$KV_NAME" \
        --query "[].{Name:name, Enabled:attributes.enabled}" \
        --output table
}

# Help
show_help() {
    echo "Usage: ./populate-secrets.sh"
    echo ""
    echo "Environment Variables:"
    echo "  KEY_VAULT_NAME     - Key Vault name (default: meatgeekkv)"
    echo "  ENVIRONMENTS       - Comma-separated environments (default: prod)"
    echo "  AZURE_SUBSCRIPTION_ID - Azure subscription ID"
    echo ""
    echo "Examples:"
    echo "  ENVIRONMENTS=prod,staging ./populate-secrets.sh"
    echo "  KEY_VAULT_NAME=meatgeekkv-test ./populate-secrets.sh"
}

case "${1:-}" in
    -h|--help)
        show_help
        exit 0
        ;;
    *)
        main "$@"
        ;;
esac