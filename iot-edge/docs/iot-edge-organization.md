# IoT Edge Folder Organization Plan

## Current State Analysis

The `iot-edge` folder currently contains a mix of production IoT Edge modules, testing infrastructure, mock devices, and various configuration files all at the root level. This makes it difficult to distinguish between production code and testing utilities.

### Current Structure Issues
- Production and test components mixed at root level
- Multiple Docker Compose files with unclear purposes
- Test device files scattered (TelemetryDirect.cs, mock-device folder, test configs)
- Deployment templates for different scenarios not clearly organized
- Documentation mixed with configuration files

## Proposed Organization Structure

```
iot-edge/
├── README.md                           # Main documentation
├── .env.example                        # Example environment template
│
├── production/                         # Production IoT Edge components
│   ├── modules/                        # Production edge modules
│   │   └── Telemetry/                 # Real telemetry module
│   │       ├── Program.cs
│   │       ├── Telemetry.csproj
│   │       ├── Dockerfile.amd64
│   │       ├── Dockerfile.arm32v7
│   │       ├── Dockerfile.arm64v8
│   │       └── module.json
│   │
│   ├── deployments/                   # Production deployment manifests
│   │   ├── deployment.template.json   # Main production template
│   │   └── config/                    # Generated deployment configs
│   │       ├── deployment.amd64.json
│   │       ├── deployment.arm32v7.json
│   │       └── deployment.arm64v8.json
│   │
│   └── scripts/                       # Production scripts
│       └── getlogs.sh                 # Production log collection
│
├── test-device/                        # All test device related components
│   ├── README.md                      # Test device documentation
│   ├── .env.azure.example              # Azure test environment template
│   │
│   ├── mock-device/                   # Mock BBQ device simulator
│   │   ├── Program.cs
│   │   ├── MockDevice.csproj
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── MockSmokerStatus.cs
│   │   ├── Services/
│   │   │   ├── ICookingScenario.cs
│   │   │   ├── SimulationUpdateService.cs
│   │   │   └── TelemetrySimulator.cs
│   │   └── module.json
│   │
│   ├── telemetry-direct/              # Direct IoT Hub connection module
│   │   ├── TelemetryDirect.cs
│   │   ├── TelemetryDirect.csproj
│   │   └── Dockerfile
│   │
│   ├── deployments/                   # Test deployment configurations
│   │   ├── docker-compose.yml         # Main test compose file
│   │   ├── docker-compose.azure.yml   # Azure IoT Hub test
│   │   ├── docker-compose.local.yml   # Local testing only
│   │   └── deployment.test.json       # IoT Edge test deployment
│   │
│   ├── scripts/                       # Test automation scripts
│   │   ├── start-test-device.sh      # Quick start script
│   │   ├── setup-azure-device.sh     # Azure device setup
│   │   └── cleanup.sh                # Clean up test resources
│   │
│   └── docs/                          # Test device documentation
│       ├── setup-guide.md            # Setup instructions
│       ├── testing-scenarios.md      # Test scenarios
│       └── troubleshooting.md        # Common issues
│
├── integration-tests/                  # Integration test suite
│   ├── docker-compose.integration.yml
│   ├── test-runner.sh
│   └── test-results/                  # Test output directory
│
├── unit-tests/                        # Unit test projects
│   └── MockDevice.Tests/
│       ├── MockDevice.Tests.csproj
│       ├── MockDeviceApiTests.cs
│       ├── MockDeviceModelTests.cs
│       └── TelemetrySimulatorTests.cs
│
├── development/                       # Development tools and configs
│   ├── deployment.debug.template.json
│   ├── config/
│   │   ├── deployment.debug.amd64.json
│   │   └── deployment.debug.arm32v7.json
│   └── docker-compose.debug.yml
│
└── docs/                             # General documentation
    ├── architecture.md               # System architecture
    ├── deployment-guide.md           # Production deployment
    └── development-guide.md          # Development setup

```

## Migration Plan

### Phase 1: Create New Directory Structure
1. Create main directories: `production/`, `test-device/`, `development/`, `unit-tests/`
2. Create subdirectories as outlined above
3. Do not move files yet, just create structure

### Phase 2: Move Production Components
1. Move `modules/Telemetry/` → `production/modules/Telemetry/`
2. Move `deployment.template.json` → `production/deployments/`
3. Move generated configs → `production/deployments/config/`
4. Move `getlogs.sh` → `production/scripts/`

### Phase 3: Consolidate Test Device Components
1. Move `mock-device/` → `test-device/mock-device/`
2. Move `TelemetryDirect.cs` and `.csproj` → `test-device/telemetry-direct/`
3. Move `Dockerfile.telemetry-direct` → `test-device/telemetry-direct/Dockerfile`
4. Move test Docker Compose files → `test-device/deployments/`
5. Move `start-test-device.sh` → `test-device/scripts/`
6. Move test environment files → `test-device/`

### Phase 4: Organize Testing Infrastructure
1. Move `mock-device-tests/` → `unit-tests/MockDevice.Tests/`
2. Keep `integration-tests/` at root level (cross-cutting concern)
3. Move debug deployment templates → `development/`

### Phase 5: Documentation Reorganization
1. Move `iot-start-test-device-azure.md` → `test-device/docs/setup-guide.md`
2. Move `iot-test-device-plan.md` → `test-device/docs/testing-scenarios.md`
3. Create new README files for each major directory
4. Update main README with new structure

### Phase 6: Update References
1. Update all Docker Compose files with new paths
2. Update Dockerfiles with correct build contexts
3. Update scripts with new directory references
4. Update CI/CD pipelines if any
5. Update import statements in code files

## Benefits of New Structure

### 1. Clear Separation of Concerns
- Production code isolated from test infrastructure
- Easy to identify what goes to production
- Test utilities grouped together logically

### 2. Improved Discoverability
- New developers can quickly understand project structure
- Related components are co-located
- Documentation is contextual to each component

### 3. Simplified Deployment
- Production deployments only need `production/` folder
- Test devices have self-contained setup in `test-device/`
- Development tools isolated in `development/`

### 4. Better Maintenance
- Changes to test infrastructure don't affect production
- Unit tests clearly separated from integration tests
- Easier to add new test scenarios or mock devices

### 5. Scalability
- Easy to add new production modules
- Simple to create additional test device types
- Clear pattern for adding new deployment scenarios

## Implementation Considerations

### Backward Compatibility
- Keep symbolic links at root for critical files during transition
- Update documentation with deprecation notices
- Provide migration script for existing deployments

### Git History Preservation
- Use `git mv` commands to preserve file history
- Create single commit for reorganization
- Tag repository before and after migration

### Team Communication
- Document changes in CHANGELOG
- Update team wikis/documentation
- Provide training on new structure

### CI/CD Updates Required
1. GitHub Actions workflow paths
2. Docker build contexts
3. Deployment scripts
4. Test runner configurations


## Recommendation

Proceed with the proposed structure (main plan) as it provides:
- Best balance of organization and practicality
- Clear separation without over-engineering
- Maintains familiar patterns (modules/, deployments/)
- Supports both production and development workflows

## Next Steps

1. Review and approve organization plan
2. Create migration script to automate file moves
3. Test all components after reorganization
4. Update documentation
5. Communicate changes to team
6. Monitor for issues during transition period

## File Mapping Reference

| Current Location | New Location |
|-----------------|--------------|
| `/modules/Telemetry/` | `/production/modules/Telemetry/` |
| `/mock-device/` | `/test-device/mock-device/` |
| `/TelemetryDirect.cs` | `/test-device/telemetry-direct/TelemetryDirect.cs` |
| `/docker-compose.azure-test.yml` | `/test-device/deployments/docker-compose.azure.yml` |
| `/deployment.template.json` | `/production/deployments/deployment.template.json` |
| `/deployment.test.template.json` | `/test-device/deployments/deployment.test.json` |
| `/start-test-device.sh` | `/test-device/scripts/start-test-device.sh` |
| `/.env.azure` | `/test-device/.env.azure.example` |
| `/mock-device-tests/` | `/unit-tests/MockDevice.Tests/` |
| `/integration-tests/` | `/integration-tests/` (no change) |
| `/deployment.debug.template.json` | `/development/deployment.debug.template.json` |

This organization will make the IoT Edge project more maintainable, understandable, and scalable for future development.