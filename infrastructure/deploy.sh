#!/bin/bash

# MeatGeek Infrastructure Deployment Script
# This script deploys the entire MeatGeek system from scratch

set -e

# Configuration
SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID:-""}
LOCATION=${AZURE_LOCATION:-"westus2"}
OBJECT_ID=${AZURE_OBJECT_ID:-""}
ENVIRONMENTS=${ENVIRONMENTS:-"prod-only"}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI is not installed"
        exit 1
    fi
    
    if [ -z "$SUBSCRIPTION_ID" ]; then
        log_error "AZURE_SUBSCRIPTION_ID environment variable is not set"
        exit 1
    fi
    
    if [ -z "$OBJECT_ID" ]; then
        log_error "AZURE_OBJECT_ID environment variable is not set"
        exit 1
    fi
    
    log_info "Prerequisites check passed"
}

# Login to Azure
azure_login() {
    log_info "Checking Azure login status..."
    
    if ! az account show &> /dev/null; then
        log_info "Not logged in to Azure, initiating login..."
        az login
    fi
    
    log_info "Setting subscription to $SUBSCRIPTION_ID"
    az account set --subscription "$SUBSCRIPTION_ID"
}

# Deploy infrastructure
deploy_infrastructure() {
    log_info "Deploying MeatGeek infrastructure..."
    log_info "Environments: $ENVIRONMENTS"
    log_info "Location: $LOCATION"
    
    DEPLOYMENT_NAME="meatgeek-$(date +%Y%m%d-%H%M%S)"
    
    log_info "Starting deployment: $DEPLOYMENT_NAME"
    
    az deployment sub create \
        --location "westus2" \
        --template-file main.bicep \
        --parameters \
            location="westus2" \
            objectId="$OBJECT_ID" \
            environmentsTodeploy="$ENVIRONMENTS" \
        --name "$DEPLOYMENT_NAME" \
        --verbose
    
    if [ $? -eq 0 ]; then
        log_info "✅ Deployment completed successfully!"
        
        # Get outputs
        log_info "Deployment outputs:"
        az deployment sub show \
            --name "$DEPLOYMENT_NAME" \
            --query "properties.outputs" \
            --output table
    else
        log_error "❌ Deployment failed!"
        exit 1
    fi
}

# Main execution
main() {
    echo "🚀 MeatGeek Infrastructure Deployment"
    echo "======================================"
    
    check_prerequisites
    azure_login
    deploy_infrastructure
    
    log_info "🎉 MeatGeek system deployment completed!"
    echo ""
    echo "Next steps:"
    echo "1. Configure secrets in Key Vault"
    echo "2. Deploy application code using GitHub Actions"
    echo "3. Configure DNS and SSL certificates"
    echo "4. Set up monitoring and alerts"
}

# Help function
show_help() {
    echo "MeatGeek Infrastructure Deployment Script"
    echo ""
    echo "Usage: ./deploy.sh [options]"
    echo ""
    echo "Environment Variables:"
    echo "  AZURE_SUBSCRIPTION_ID  - Azure subscription ID (required)"
    echo "  AZURE_OBJECT_ID        - Azure AD object ID for Key Vault (required)"
    echo "  AZURE_LOCATION         - Deployment location (default: westus2)"
    echo "  ENVIRONMENTS           - Environments to deploy (default: prod-only)"
    echo ""
    echo "Environment Options:"
    echo "  prod-only              - Deploy production only"
    echo "  prod-staging           - Deploy production and staging"
    echo "  prod-staging-test      - Deploy production, staging, and test"
    echo "  all                    - Deploy all environments"
    echo ""
    echo "Examples:"
    echo "  ENVIRONMENTS=prod-staging ./deploy.sh"
    echo "  AZURE_LOCATION=eastus ENVIRONMENTS=all ./deploy.sh"
    echo ""
    echo "Options:"
    echo "  -h, --help            Show this help message"
}

# Parse command line arguments
case "${1:-}" in
    -h|--help)
        show_help
        exit 0
        ;;
    *)
        main "$@"
        ;;
esac