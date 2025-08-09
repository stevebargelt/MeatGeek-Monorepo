# MeatGeek Monorepo - Claude AI Assistant Guide

This document provides instructions for Claude AI to effectively work with the MeatGeek monorepo.

## Project Overview

MeatGeek is a .NET 8.0 microservices monorepo for a BBQ/Grilling IoT platform. The solution uses Nx for build orchestration and consists of:

- **Sessions API**: Azure Functions for managing cooking sessions
- **Device API**: Azure Functions for device communication via Azure Relay
- **IoT Functions**: Telemetry processing and data management
- **IoT Edge**: Edge computing modules for device telemetry
- **Shared Library**: Common utilities and event schemas

## Key Commands

### Build Commands
```bash
# Build all projects
nx run-many -t build

# Build specific project
nx build MeatGeek.Sessions.Api
nx build MeatGeek.Device.Api
nx build MeatGeek.IoT.Functions
nx build MeatGeek.Shared
```

### Test Commands
```bash
# Run all tests
nx run-many -t test

# Test specific project
nx test MeatGeek.Sessions.Api.Tests
nx test MeatGeek.Device.Api.Tests
nx test MeatGeek.IoT.Functions.Tests
nx test MeatGeek.Shared.Tests
```

### Lint Commands
```bash
# Lint all projects
nx run-many -t lint

# Format code
nx format
```

### Local Development
```bash
# Serve Azure Functions locally
nx serve MeatGeek.Sessions.Api
nx serve MeatGeek.Device.Api
nx serve MeatGeek.IoT.Functions
```

## Project Structure

```
/
├── sessions/           # Sessions API microservice
│   ├── src/
│   │   ├── MeatGeek.Sessions.Api/          # Azure Functions
│   │   ├── MeatGeek.Sessions.Services/     # Business logic
│   │   ├── MeatGeek.Sessions.WorkerApi/    # Event-driven workers
│   │   └── *.Tests/                        # Unit tests
│   └── deploy/        # Bicep templates
├── device/            # Device API microservice
│   └── src/
│       ├── MeatGeek.Device.Api/            # Azure Functions
│       └── MeatGeek.Device.Api.Tests/      # Unit tests
├── iot/               # IoT microservice
│   └── src/
│       ├── MeatGeek.IoT.Functions/         # Azure Functions
│       ├── MeatGeek.IoT.WorkerApi/         # Event-driven workers
│       └── *.Tests/                        # Unit tests
├── iot-edge/          # IoT Edge modules
│   └── modules/
│       └── Telemetry/                      # Telemetry module
├── shared/            # Shared library
│   └── src/
│       └── MeatGeek.Shared/                # Common code
└── .github/workflows/ # CI/CD pipelines
```

## Testing Strategy

1. **Always run tests before committing code**
2. **Unit tests are required for all new functionality**
3. **Test projects follow naming convention: `{ProjectName}.Tests`**
4. **Use xUnit for testing**
5. **Mock external dependencies (Azure services, databases)**

## Code Quality Standards

1. **C# 12 / .NET 8.0 features are preferred**
2. **Follow existing code patterns and conventions**
3. **Use dependency injection**
4. **Implement proper error handling and logging**
5. **Azure Functions should be stateless**
6. **Use async/await for I/O operations**

## Git Commit Guidelines
- Please use Conventional Commits formatting for git commits.
- Please use Conventional Branch naming (prefix-based branch naming convention).
- Please do not mention yourself (Claude) as a co-author when committing, or include any links to Claude Code.

## Visual Development Memories
- Please use playwright MCP swerver when making visual changes to the front-end to check your work.

## Guidance Memories
- Please ask for clarification upfront, upon initial prompts, when you need more direction. 

## Documentation Memories
- Please use context7 to find the relevant, up-to-date documentation when working with 3rd party libraries, packages, frameworks, etc. as needed.

## Azure Resources

### Resource Groups
- `MeatGeek-Sessions`: Sessions API resources
- `MeatGeek-Device`: Device API resources  
- `MeatGeek-IoT`: IoT Functions resources
- `MeatGeek-Shared`: Shared resources (Event Grid, Key Vault)
- `MeatGeek-Proxy`: API proxy resources

### Key Services
- **Azure Functions**: All APIs run as Function Apps
- **Event Grid**: Inter-service communication
- **Key Vault**: Secret management
- **Application Insights**: Monitoring and diagnostics
- **Azure Relay**: Device communication

## Common Tasks

### Adding a New Azure Function
1. Create the function class in the appropriate project
2. Add unit tests in the corresponding test project
3. Update the Startup.cs if new dependencies are needed
4. Run `nx test {project-name}` to verify
5. Update the deployment bicep files if needed

### Updating Event Schemas
1. Modify schema in `shared/src/MeatGeek.Shared/Event Schemas/`
2. Update EventTypes.cs if adding new event types
3. Run tests: `nx test MeatGeek.Shared.Tests`
4. Update consuming services to handle new schema

### Working with IoT Edge
1. Modules are in `iot-edge/modules/`
2. Use deployment templates for configuration
3. Build with: `nx build Telemetry`

## CI/CD Considerations

- **GitHub Actions** handle all deployments
- **Tests must pass** before deployment (enhanced workflows)
- **Deployment secrets** are in GitHub repository secrets
- **Function App publish profiles** are required for deployment

## Important Notes

1. **NEVER commit secrets or connection strings**
2. **Always use Key Vault references in production**
3. **Check for duplicate workflows** (e.g., sessions-build-deploy.yml vs sessions-build-deploy-enhanced.yml)
4. **Run format/lint before creating PRs**
5. **IoT Edge deployments require device twin updates** for log collection

## Debugging Tips

1. Use `nx serve` for local Azure Functions development
2. Check Application Insights for production issues
3. Use `nx graph` to visualize project dependencies
4. Clear Nx cache with `nx reset` if builds are inconsistent

