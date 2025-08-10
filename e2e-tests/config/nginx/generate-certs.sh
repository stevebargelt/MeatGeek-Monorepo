#!/bin/bash
# Generate self-signed certificates for local Edge Hub simulation

set -e

echo "🔐 Generating self-signed certificates for local Edge Hub..."

# Create certificates directory if it doesn't exist
mkdir -p /tmp/edgehub-certs

# Generate private key
openssl genrsa -out /tmp/edgehub-certs/key.pem 2048

# Generate certificate
openssl req -new -x509 -key /tmp/edgehub-certs/key.pem -out /tmp/edgehub-certs/cert.pem -days 365 -subj "/CN=local-edgehub"

echo "✅ Certificates generated:"
echo "  Certificate: /tmp/edgehub-certs/cert.pem"
echo "  Private Key: /tmp/edgehub-certs/key.pem"