# Azure Functions Isolated Mode Deployment Fixes

## Problem Summary
MeatGeek.Sessions.Api Azure Functions app is not working in Azure after switching to .NET 8 isolated mode. Azure errors still reference `startup.cs` which was deleted during the migration.

## Root Cause Analysis

### 1. **Program.cs Configuration Issues**
**Problem**: Almost all essential services are commented out in Program.cs, making the application non-functional.

**Current State**:
```csharp
// ALL SERVICES COMMENTED OUT:
// - Application Insights
// - CosmosDB Client  
// - Dependency Injection for Services
// - Logging Configuration
```

**Impact**: Functions can't resolve dependencies like `ISessionsService`, `ISessionsRepository`, causing runtime failures.

### 2. **Missing host.json Configuration for Isolated Mode**
**Problem**: host.json lacks isolated worker mode specific settings.

**Current host.json**: Standard v2 configuration
**Missing**: 
- Worker-specific configurations
- Custom handlers for isolated mode
- Proper extension bundles

### 3. **Deployment Pipeline Issues**
**Problem**: sessions-build-deploy-enhanced.yml may not be properly configured for isolated worker deployment.

**Potential Issues**:
- Build output path might be incorrect for isolated mode
- Missing isolated worker mode publish settings
- Package structure not matching Azure Functions expectations

### 4. **Azure Cache/Startup.cs Reference**
**Problem**: Azure still references deleted Startup.cs, indicating stale deployment artifacts.

**Root Cause**: Likely incomplete deployment or Azure Function App configuration still pointing to legacy mode.

## Immediate Fixes

### Fix 1: Restore Program.cs Configuration
**Priority**: CRITICAL

Uncomment and fix the essential services in Program.cs:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Application Insights
        var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
        }

        // CosmosDB Client
        services.AddSingleton<CosmosClient>((s) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("CosmosDBConnection", "Please specify CosmosDBConnection in Azure Functions Settings.");
            }

            var cosmosDbConnectionString = new CosmosDbConnectionString(connectionString);
            return new CosmosClientBuilder(
                cosmosDbConnectionString.ServiceEndpoint?.OriginalString ?? throw new ArgumentNullException("ServiceEndpoint is null"), 
                cosmosDbConnectionString.AuthKey)
                .WithBulkExecution(true)
                .Build();
        });

        // Services
        services.AddScoped<ISessionsService, SessionsService>();
        services.AddScoped<ISessionsRepository, SessionsRepository>();
        services.AddScoped<IEventGridPublisherService, EventGridPublisherService>();
    })
    .Build();

host.Run();
```

### Fix 2: Update host.json for Isolated Mode
**Priority**: HIGH

Add isolated worker specific configuration:

```json
{
  "version": "2.0",
  "functionTimeout": "00:05:00",
  "logging": {
    "fileLoggingMode": "debugOnly",
    "logLevel": {
      "MeatGeek.Sessions": "Information",
      "MeatGeek.Sessions.Services": "Information", 
      "default": "Information"
    }
  },
  "customHandler": {
    "description": {
      "defaultExecutablePath": "MeatGeek.Sessions.Api.exe",
      "workingDirectory": "",
      "arguments": []
    },
    "enableForwardingHttpRequest": false
  },
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[4.*, 5.0.0)"
  }
}
```

### Fix 3: Fix Deployment Pipeline
**Priority**: HIGH

Update sessions-build-deploy-enhanced.yml build steps:

**Current Issue** (lines 122-124):
```yaml
pushd './${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}'
dotnet build "./sessions/src/MeatGeek.Sessions.Api" --configuration Release --output ./output
popd
```

**Proposed Fix**:
```yaml
- name: "Resolve Project Dependencies Using Dotnet"
  shell: bash
  run: |
    pushd './sessions/src/MeatGeek.Sessions.Api'
    dotnet restore
    dotnet publish --configuration Release --output ../../../output --no-restore
    popd

- name: "Run Azure Functions Action"
  uses: Azure/functions-action@v1
  id: fa
  with:
    app-name: ${{ env.AZURE_FUNCTIONAPP_NAME_PROD }}
    package: "./output"
    publish-profile: ${{ secrets.PUBLISH_PROFILE_SESSIONS_API_FUNCTION_APP }}
```

### Fix 4: Clear Azure Function App Cache
**Priority**: MEDIUM

**Azure Portal Steps**:
1. Go to Azure Function App `meatgeeksessionsapi`
2. Navigate to "Development Tools" → "Advanced Tools (Kudu)"
3. Click "Go" to open Kudu console
4. Delete contents of `D:\home\site\wwwroot\`
5. Restart the Function App
6. Redeploy

**Alternative via Azure CLI**:
```bash
az functionapp restart --name meatgeeksessionsapi --resource-group MeatGeek-Sessions
```

## Additional Configuration Checks

### Check 1: Azure Function App Settings
Verify these Application Settings exist in Azure:
- `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- `FUNCTIONS_EXTENSION_VERSION` = `~4`
- `CosmosDBConnection` = `[Your CosmosDB Connection String]`
- `APPLICATIONINSIGHTS_CONNECTION_STRING` = `[Your App Insights Connection String]`

### Check 2: Publish Profile Compatibility
Ensure publish profile (`PUBLISH_PROFILE_SESSIONS_API_FUNCTION_APP`) is configured for:
- .NET 8.0
- Isolated worker mode
- Correct runtime stack

### Check 3: Function App Runtime Configuration
In Azure Portal → Function App → Configuration:
- **Runtime version**: `~4`
- **Runtime stack**: `.NET`
- **Version**: `8 (LTS), isolated worker model`

## Testing Strategy

### 1. Local Testing First
```bash
# Restore Program.cs services
# Update host.json
cd sessions/src/MeatGeek.Sessions.Api
func start --dotnet-isolated-debug
```

### 2. Staging Deployment Test
```bash
# Deploy to staging first
git checkout develop
git commit -am "fix: restore Program.cs services for isolated mode"
git push origin develop
# Monitor staging deployment
```

### 3. Production Deployment
```bash
# Only after staging success
git checkout main
git merge develop
git push origin main
```

## Monitoring and Validation

### Application Insights Queries
```kusto
// Check for startup errors
traces
| where timestamp > ago(1h)
| where severityLevel >= 3
| order by timestamp desc

// Check function executions
requests
| where timestamp > ago(1h)
| summarize count() by name, resultCode
```

### Health Check Endpoints
After deployment, test these endpoints:
- `GET /api/sessions/{smokerId}` (GetAllSessions)
- `POST /api/sessions` (CreateSession)
- `GET /api/sessions/{smokerId}/{id}` (GetSession)

## Rollback Plan

If fixes don't work:
1. Revert Program.cs to minimal working state
2. Create new Function App with isolated mode from scratch
3. Migrate configuration and secrets
4. Update deployment pipeline to target new Function App

## Success Criteria

✅ **Deployment Succeeds**: No build/deployment errors
✅ **Functions Start**: Application Insights shows successful startup
✅ **Dependencies Resolve**: No DI container errors
✅ **API Responds**: Health check endpoints return expected responses
✅ **No Startup.cs References**: Azure logs show no legacy references

## Next Steps Priority Order

1. **IMMEDIATE**: Restore Program.cs services (Fix 1)
2. **HIGH**: Update deployment pipeline (Fix 3)  
3. **HIGH**: Update host.json (Fix 2)
4. **MEDIUM**: Clear Azure cache (Fix 4)
5. **LOW**: Validate configuration settings

**Estimated Time to Resolution**: 2-4 hours including testing