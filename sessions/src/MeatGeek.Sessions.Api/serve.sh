#!/bin/bash
set -e

echo "Building MeatGeek.Sessions.Api..."
dotnet build --output ./bin/output --configuration Debug

echo "Starting Azure Functions..."
cd bin/output
func start --csharp