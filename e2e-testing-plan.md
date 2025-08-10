# MeatGeek-Monorepo E2E Testing Plan

## Overview

This plan establishes a comprehensive End-to-End testing framework for the MeatGeek BBQ IoT platform, focusing on validating complete workflows across all microservices, IoT edge components, and Azure resources. The testing strategy emphasizes API testing, service integration validation, IoT pipeline testing, and functional validation of the entire distributed system.

**Key Focus Areas:**
- Complete cooking session workflows from device connection to data persistence
- Inter-service communication via Event Grid and Azure messaging
- IoT Edge telemetry pipeline validation with real Azure IoT Hub integration
- Error recovery and resilience testing across service boundaries
- API contract validation and authentication testing

## Testing Framework Architecture

### Primary Technology Stack

**Core Framework: Jest + Supertest + TestContainers + Azure SDK**
- **Jest**: Industry-standard test runner and assertion framework for Node.js
- **Supertest**: Purpose-built HTTP testing library optimized for API validation
- **TestContainers**: Local service dependencies (Cosmos DB, Event Grid emulator)
- **Azure SDK**: Real Azure resource integration and validation
- **Docker Compose**: Local environment orchestration
- **Allure Reports**: Rich test reporting with detailed analytics

**Rationale for Technology Choices:**
- **Jest + Supertest**: Proven combination for API testing with excellent Node.js ecosystem integration, lightweight execution, and comprehensive assertion capabilities
- **TestContainers**: Enables realistic local testing with actual service dependencies
- **Azure SDK**: Provides authentic cloud service integration for production-like validation
- **Builds on Existing**: Extends proven IoT Edge integration tests rather than replacing them

### Test Environment Strategy

**Integration with Existing IoT Edge Tests:**
- **Extends existing**: `iot-edge/integration-tests/test-runner.sh` (proven, working solution)
- **Reuses MockDevice**: Leverages existing realistic BBQ simulation with 24 passing unit tests
- **Builds on Docker Compose**: Extends `docker-compose.test.yml` for full-stack scenarios

**Local Development:**
- **Foundation**: Existing IoT Edge integration testing framework
- **Enhancement**: Add cross-service workflow validation on top of proven base
- **Emulators**: TestContainers for Cosmos DB, Event Grid emulator where beneficial
- **Real Services**: Existing IoT Edge runtime integration

**Staging/CI:**
- **Hybrid approach**: Proven local integration tests + real Azure for cross-service validation
- **Cost-effective**: Shared Azure test resources with isolated test data
- **Automated cleanup**: Resource lifecycle management to control costs


## Test Categories & Coverage

**Testing Layer Architecture:**
```
E2E Testing Layers (Building on Existing):
├── Unit Tests (existing - 24 MockDevice tests passing)
├── Component Tests (existing - individual service tests)
├── IoT Integration Tests (existing - iot-edge/integration-tests) ✅ PROVEN
└── End-to-End Workflows (new - complete business journeys)
```

### 1. True End-to-End User Workflows (NEW FOCUS)
Validates complete business workflows that span multiple services and represent real user value.

**Focus:** Actual BBQ enthusiast journeys from device setup to cooking completion and data analysis.

### 2. Cross-Service Integration Tests (EXTENDS EXISTING)
Validates communication between microservices through Event Grid, building on existing IoT Edge tests.

**Approach:** Extends proven `iot-edge/integration-tests/test-runner.sh` with cross-service validation.

### 3. IoT Pipeline Validation (REUSES EXISTING)
Leverages existing comprehensive IoT Edge integration testing framework.

**Foundation:** Proven `docker-compose.integration.yml` and MockDevice simulation (already working).

### 4. System Resilience & Recovery Tests (NEW)
Validates system behavior under failure conditions and recovery scenarios.

**Focus:** Chaos engineering and production-like failure simulation.

### 5. Security & Compliance Validation (NEW)
Tests authentication, authorization, and data protection across service boundaries.

**Focus:** End-to-end security workflows rather than individual API security.

## Files to Create/Modify

### New Files Structure (Building on Existing)
```
/e2e-tests/
├── jest.config.js                    # Jest configuration for E2E tests
├── package.json                      # E2E test dependencies (Jest + Supertest)
├── setup/
│   ├── global-setup.ts               # Extends existing iot-edge integration setup
│   ├── global-teardown.ts            # Cleanup after tests
│   └── azure-resources.bicep         # Test Azure resources
├── fixtures/                         # Test data fixtures
│   ├── bbq-scenarios.json            # Realistic cooking scenarios
│   ├── user-workflows.json           # End-to-end user journeys
│   └── test-sessions.json            # Session test data
├── utils/                            # Shared test utilities
│   ├── existing-test-runner.ts       # Wraps iot-edge/integration-tests/test-runner.sh
│   ├── azure-client.ts               # Azure SDK integration
│   ├── workflow-orchestrator.ts      # E2E workflow coordination
│   ├── test-data-factory.ts          # Test data generation
│   └── mock-device-controller.ts     # Controls existing MockDevice simulation
└── tests/                            # Test suites (focused on TRUE E2E)
    ├── user-workflows/               # Complete business workflows
    │   ├── bbq-enthusiast-12-hour-brisket.spec.ts
    │   ├── multi-device-family-cookout.spec.ts
    │   └── session-recovery-network-failure.spec.ts
    ├── cross-service-integration/    # Extends existing IoT integration
    │   ├── session-telemetry-sync.spec.ts
    │   └── event-driven-workflows.spec.ts
    └── system-resilience/            # Production failure scenarios
        ├── chaos-engineering.spec.ts
        ├── azure-service-outages.spec.ts
        └── long-running-stability.spec.ts
```

### Integration Points with Existing Code
```
# Extends Rather Than Replaces:
/iot-edge/integration-tests/          # ✅ KEEP - Proven integration tests
├── test-runner.sh                    # ✅ EXTEND - Use as foundation
├── docker-compose.integration.yml    # ✅ EXTEND - Add to orchestration
└── [existing test infrastructure]    # ✅ BUILD UPON

/iot-edge/mock-device/                # ✅ REUSE - 24 passing tests
├── Services/TelemetrySimulator.cs    # ✅ LEVERAGE - Realistic BBQ simulation
├── project.json                      # ✅ INTEGRATE - Nx build system
└── [proven mock device implementation] # ✅ UTILIZE
```

### Modified Files
```
/package.json                         # Add E2E test scripts (extends existing)
/nx.json                             # Add E2E test project (integrates with MockDevice)
/.github/workflows/e2e-tests.yml     # CI/CD pipeline (builds on existing tests)
/infrastructure/test-resources.bicep # Test environment resources
/iot-edge/integration-tests/         # EXTEND existing with cross-service validation
    test-runner.sh                    # Add E2E workflow orchestration
```

## Core Function Specifications

### Environment Management Functions

**`setupTestEnvironment()`**
Orchestrates the complete test environment including Azure resources, local services, and test data preparation. Handles resource provisioning, service health validation, and test data seeding.

**`teardownTestEnvironment()`**
Performs comprehensive cleanup including Azure resource deletion, local container cleanup, and test data purging. Ensures cost control and environment isolation.

**`waitForServicesHealthy()`**
Implements robust health checking with retries and timeout handling. Validates all required services are ready before test execution.

### Azure Integration Functions

**`provisionTestAzureResources()`**
Creates isolated Azure resources for testing including IoT Hub, Cosmos DB, Function Apps, and Event Grid topics. Uses Bicep templates for consistent provisioning.

**`validateAzureResourceConnectivity()`**
Tests connectivity to all Azure services and validates proper configuration. Ensures authentication, network access, and service availability.

**`cleanupAzureTestResources()`**
Removes all test Azure resources to prevent cost accumulation. Implements safety checks to avoid accidental production resource deletion.

### Integration Functions (Building on Existing)

**`runExistingIoTEdgeTests()`**
Executes proven `iot-edge/integration-tests/test-runner.sh` as foundation for E2E workflows. Validates IoT Edge pipeline is working before cross-service testing.

**`leverageMockDeviceSimulation()`**
Utilizes existing MockDevice with 24 passing unit tests and realistic BBQ physics simulation. Extends with cross-service workflow validation.

**`orchestrateExistingInfrastructure()`**
Extends proven `docker-compose.integration.yml` with additional services for full workflow testing. Builds on working foundation.

### True E2E Workflow Functions

**`createCompleteUserJourney()`**
Orchestrates end-to-end user workflows: BBQ enthusiast creates session → device connects → cooks → monitors → analyzes results. Spans all services.

**`validateBusinessWorkflows()`**
Ensures complete business scenarios work correctly: multi-device family cookouts, session recovery after failures, long cooking sessions.

**`traceDataFlowAcrossServices()`**
Tracks data from device telemetry through Sessions API, IoT Functions, Event Grid, and final storage. Validates complete system integration.

### Cross-Service Integration Functions

**`validateEventDrivenWorkflows()`**
Traces Event Grid messages throughout complete workflows ensuring proper routing between Sessions API, IoT Functions, and Device API.

**`testServiceBoundaryIntegration()`**
Validates communication between microservices that existing unit/integration tests cannot cover. Focuses on service interaction edge cases.


## Test Specifications (True E2E Focus)

### Complete User Journey Tests (NEW FOCUS)

**`test_bbq_enthusiast_complete_12_hour_brisket_journey`**
**True E2E Value:** Tests complete user workflow from session creation through cooking to final analysis.
```typescript
// 1. User creates session via Sessions API
// 2. MockDevice (existing) simulates realistic 12-hour brisket cook
// 3. Telemetry flows through IoT Edge → IoT Hub → IoT Functions
// 4. Session data persists correctly in Cosmos DB
// 5. User can query session history and analysis
// 6. All Event Grid events fire correctly throughout workflow
```

**`test_family_cookout_multi_device_coordination`**
**True E2E Value:** Tests complex scenario with multiple devices and concurrent sessions.
```typescript
// Leverages existing MockDevice.Tests (24 passing) for multiple device simulation
// Validates cross-service coordination at scale
```

**`test_cooking_session_network_failure_recovery`**
**True E2E Value:** Tests system resilience during real user scenarios.
```typescript
// Uses existing docker-compose.integration.yml as foundation
// Adds network failure simulation to test complete recovery workflow
```

### Cross-Service Integration Tests (EXTENDS EXISTING)

**`test_event_driven_workflow_complete_session_lifecycle`**
**Building on Existing:** Extends proven IoT Edge integration tests with cross-service Event Grid validation.
```typescript
// Foundation: Existing iot-edge/integration-tests/test-runner.sh
// Enhancement: Add Sessions API and IoT Functions Event Grid validation
// Focus: End-to-end event flow rather than individual event routing
```

**`test_data_consistency_across_service_boundaries`**
**Leverages MockDevice:** Uses existing realistic BBQ simulation to generate consistent test data.
```typescript
// Foundation: MockDevice.Tests (24 passing tests provide data reliability)
// Enhancement: Validate data consistency across Sessions API and IoT Functions
// Focus: Cross-service data integrity rather than individual service validation
```

### IoT Pipeline Validation (REUSES EXISTING)

**`test_extended_iot_pipeline_with_cross_service_validation`**
**Foundation:** Proven `iot-edge/integration-tests/test-runner.sh` (already working).
```typescript
// REUSE: Existing IoT Edge integration tests (comprehensive telemetry validation)
// EXTEND: Add cross-service validation after IoT pipeline completes
// FOCUS: Integration points not covered by existing tests
```

**`test_realistic_bbq_telemetry_through_complete_system`**
**Foundation:** MockDevice simulation with realistic BBQ physics (already implemented).
```typescript
// REUSE: TelemetrySimulator.cs with Brisket/PorkShoulder/Ribs scenarios
// EXTEND: Validate complete data flow through all services
// FOCUS: End-to-end telemetry journey validation
```

**Note:** Individual IoT pipeline components (device twins, direct methods, etc.) are already comprehensively tested by existing `iot-edge/integration-tests/`. E2E tests focus on cross-service integration points.

### System Resilience Tests (NEW FOCUS)

**`test_production_like_failure_scenarios`**
**True E2E Value:** Tests system behavior under realistic production failure conditions.
```typescript
// Chaos engineering: Azure service outages during active cooking sessions
// Network partitions: IoT Edge disconnection and reconnection
// Data corruption: Recovery and consistency validation
```

**`test_long_running_system_stability`**
**True E2E Value:** Validates system stability over extended periods.
```typescript
// 24-hour stability test using MockDevice realistic simulation
// Memory leak detection across all services
// Connection pool management validation
```

**Note:** Individual API contract testing is more appropriate for component/integration test layers. E2E tests focus on complete workflows where API interactions serve the business journey.


### Security & Compliance Validation (NEW FOCUS)

**`test_end_to_end_security_workflows`**
**True E2E Value:** Validates security across complete user workflows rather than individual API endpoints.
```typescript
// Complete authentication flow: User → Sessions API → Device API → IoT Hub
// Data encryption validation throughout telemetry pipeline
// Authorization validation across service boundaries
```

**`test_data_protection_complete_lifecycle`**
**True E2E Value:** Validates data protection throughout complete data lifecycle.
```typescript
// Data encryption: Device → Edge → Cloud → Storage
// Data retention: Session data lifecycle management
// Data access: Cross-service authorization validation
```

## Implementation Phases (Revised)

### Phase 1: Foundation - Build on Existing (Week 1-2)
**CHANGED:** Integrate with proven existing work instead of creating parallel infrastructure.
- ✅ **Extend existing IoT Edge tests**: Enhance `iot-edge/integration-tests/test-runner.sh` with cross-service validation
- ✅ **Set up Jest + Supertest**: Replace Playwright with purpose-built API testing framework
- ✅ **Leverage MockDevice**: Integrate with existing 24 passing tests and realistic BBQ simulation  
- ✅ **Extend Docker Compose**: Build on proven `docker-compose.integration.yml`
- ✅ **Establish CI/CD integration**: Extend existing test workflows

### Phase 2: True E2E Workflows (Week 3-4)
**CHANGED:** Focus on complete business workflows rather than component testing.
- ✅ **Complete user journeys**: BBQ enthusiast workflows from session creation to analysis
- ✅ **Multi-device scenarios**: Family cookout coordination using existing MockDevice scenarios
- ✅ **Cross-service integration**: Event Grid workflows building on IoT Edge foundation
- ✅ **System boundary validation**: Service integration points not covered by existing tests

### Phase 3: Production Resilience (Week 5-6)
**CHANGED:** Focus on production-ready resilience rather than individual component testing.
- ✅ **Chaos engineering**: Production-like failure scenarios with existing infrastructure
- ✅ **Long-running stability**: 24-hour tests using MockDevice realistic simulation
- ✅ **Security workflows**: End-to-end security validation across service boundaries
- ✅ **Performance baselines**: System-wide performance validation and monitoring

## Success Metrics (Revised)

**True E2E Coverage Targets:**
- ✅ **100% critical user workflow coverage** (complete BBQ cooking journeys)
- ✅ **90%+ cross-service integration coverage** (building on existing 95% IoT pipeline coverage)
- ✅ **100% system resilience scenario coverage** (production-like failure testing)
- ✅ **Leverage existing 24 MockDevice tests** (proven realistic BBQ simulation)

**Business Value Benchmarks:**
- ✅ **Complete workflow validation**: User can successfully complete full BBQ cooking session
- ✅ **Data integrity across services**: Telemetry data consistent from device to final storage
- ✅ **System recovery validation**: Zero data loss during realistic failure scenarios
- ✅ **Cross-service event flow**: Event Grid messages properly route through complete workflows

**Quality Gates (Building on Existing):**
- ✅ **Existing IoT Edge tests pass**: Foundation must be solid (`iot-edge/integration-tests/`)
- ✅ **MockDevice tests pass**: Realistic simulation foundation (24 tests)
- ✅ **True E2E workflows pass**: Complete business scenario validation
- ✅ **Production resilience validated**: System handles realistic failure conditions

**Cost Control & Efficiency:**
- ✅ **Reuse existing infrastructure**: Minimize duplicate testing effort
- ✅ **Azure test resource management**: Automated provisioning and cleanup
- ✅ **Test execution time**: E2E tests complete within 30 minutes

## Summary of Critical Path Updates Applied

### ✅ 1. Simplified Technology Stack (Jest + Supertest)
**BEFORE:** Playwright (UI testing tool) for API testing
**AFTER:** Jest + Supertest (purpose-built API testing framework)
**BENEFIT:** Lightweight, fast execution, better Node.js ecosystem integration

### ✅ 2. Integration with Existing Excellent Work
**BEFORE:** Parallel testing infrastructure ignoring existing proven work
**AFTER:** Builds on and extends existing IoT Edge integration tests
**BENEFIT:** 
- Leverages proven `iot-edge/integration-tests/test-runner.sh`
- Reuses MockDevice with 24 passing tests and realistic BBQ simulation
- Extends `docker-compose.integration.yml` rather than replacing

### ✅ 3. Focus on True End-to-End Value
**BEFORE:** Mix of E2E, integration, and contract testing
**AFTER:** Clear focus on complete business workflows
**BENEFIT:**
- Tests actual user journeys (BBQ enthusiast complete cooking sessions)
- Validates cross-service integration not covered by existing tests
- Focuses on business value rather than technical component testing

This **refined E2E testing framework** builds on the excellent existing foundation to provide comprehensive end-to-end validation while avoiding duplication and maximizing ROI. The approach ensures the MeatGeek BBQ IoT platform delivers reliable, complete workflows for BBQ enthusiasts with cost-effective testing that leverages proven components.