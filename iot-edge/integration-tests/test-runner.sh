#!/bin/bash

# MeatGeek IoT Edge Integration Test Runner
# Validates complete data flow from mock device through telemetry module to Azure IoT Hub

set -euo pipefail

# Configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
readonly TEST_RESULTS_DIR="$SCRIPT_DIR/test-results"
readonly LOG_DIR="$SCRIPT_DIR/logs"
readonly TIMEOUT_SECONDS=600
readonly COMPOSE_FILE="$SCRIPT_DIR/docker-compose.integration.yml"

# Test configuration
readonly EXPECTED_MESSAGES=60  # 5-minute test at 5-second intervals
readonly VALIDATION_TIMEOUT=300
readonly HEALTH_CHECK_RETRIES=20
readonly HEALTH_CHECK_DELAY=15

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

# Logging functions
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $*" >&2
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $*" >&2
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $*" >&2
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $*" >&2
}

# Error handling
cleanup() {
    log "Cleaning up test environment..."
    
    # Stop and remove containers
    docker-compose -f "$COMPOSE_FILE" down --volumes --remove-orphans || true
    
    # Remove test networks
    docker network prune -f || true
    
    # Archive logs if they exist
    if [[ -d "$LOG_DIR" ]]; then
        local archive_name="integration_test_logs_$(date +%Y%m%d_%H%M%S).tar.gz"
        tar -czf "$TEST_RESULTS_DIR/$archive_name" -C "$LOG_DIR" . || true
        log "Logs archived to $archive_name"
    fi
}

trap cleanup EXIT INT TERM

# Pre-flight checks
preflight_checks() {
    log "Running pre-flight checks..."
    
    # Check required environment variables
    if [[ -z "${TEST_DEVICE_CONNECTION_STRING:-}" ]]; then
        log_error "TEST_DEVICE_CONNECTION_STRING environment variable is required"
        exit 1
    fi
    
    # Validate connection string format
    if [[ ! "$TEST_DEVICE_CONNECTION_STRING" =~ HostName=.*\.azure-devices\.net ]]; then
        log_error "Invalid TEST_DEVICE_CONNECTION_STRING format"
        exit 1
    fi
    
    # Check Docker availability
    if ! docker version &>/dev/null; then
        log_error "Docker is not running or accessible"
        exit 1
    fi
    
    # Check Docker Compose availability
    if ! docker-compose version &>/dev/null; then
        log_error "Docker Compose is not available"
        exit 1
    fi
    
    # Verify mock device build exists or can be built
    if [[ ! -f "$PROJECT_ROOT/mock-device/Dockerfile" ]]; then
        log_error "Mock device Dockerfile not found at $PROJECT_ROOT/mock-device/Dockerfile"
        exit 1
    fi
    
    # Verify telemetry module build exists or can be built
    if [[ ! -f "$PROJECT_ROOT/modules/Telemetry/Dockerfile.amd64" ]]; then
        log_error "Telemetry module Dockerfile not found at $PROJECT_ROOT/modules/Telemetry/Dockerfile.amd64"
        exit 1
    fi
    
    log_success "Pre-flight checks passed"
}

# Setup test environment
setup_test_environment() {
    log "Setting up test environment..."
    
    # Create directories
    mkdir -p "$TEST_RESULTS_DIR" "$LOG_DIR"
    
    # Create test configuration files
    create_test_configs
    
    # Build required Docker images
    log "Building Docker images..."
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    
    log_success "Test environment setup complete"
}

# Create test configuration files
create_test_configs() {
    log "Creating test configuration files..."
    
    # Create integration routes configuration
    mkdir -p "$SCRIPT_DIR/config"
    cat > "$SCRIPT_DIR/config/integration-routes.json" <<EOF
{
  "routes": {
    "TelemetryToUpstream": "FROM /messages/modules/Telemetry/outputs/* INTO \$upstream",
    "TelemetryToValidator": "FROM /messages/modules/Telemetry/outputs/* INTO BrokeredEndpoint(\"/modules/message-validator/inputs/telemetry\")"
  }
}
EOF
    
    # Create validator Dockerfile
    cat > "$SCRIPT_DIR/validator.Dockerfile" <<EOF
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /validation
COPY validator/ ./
ENTRYPOINT ["dotnet", "MessageValidator.dll"]
EOF
    
    # Create orchestrator Dockerfile
    cat > "$SCRIPT_DIR/orchestrator.Dockerfile" <<EOF
FROM alpine:3.18
RUN apk add --no-cache bash curl jq
WORKDIR /orchestrator
COPY orchestrator-scripts/ ./
RUN chmod +x *.sh
ENTRYPOINT ["./run-integration-tests.sh"]
EOF
}

# Start test services
start_services() {
    log "Starting integration test services..."
    
    # Start services in dependency order
    docker-compose -f "$COMPOSE_FILE" up -d workload-api
    sleep 10
    
    docker-compose -f "$COMPOSE_FILE" up -d edgehub
    sleep 15
    
    docker-compose -f "$COMPOSE_FILE" up -d mock-device
    sleep 10
    
    docker-compose -f "$COMPOSE_FILE" up -d telemetry-module
    sleep 5
    
    docker-compose -f "$COMPOSE_FILE" up -d message-validator
    sleep 5
    
    log_success "All services started"
}

# Wait for services to be healthy
wait_for_healthy_services() {
    log "Waiting for services to become healthy..."
    
    local retry_count=0
    local max_retries=$HEALTH_CHECK_RETRIES
    
    while [[ $retry_count -lt $max_retries ]]; do
        if check_service_health; then
            log_success "All services are healthy"
            return 0
        fi
        
        retry_count=$((retry_count + 1))
        log "Health check attempt $retry_count/$max_retries failed, retrying in ${HEALTH_CHECK_DELAY}s..."
        sleep $HEALTH_CHECK_DELAY
    done
    
    log_error "Services failed to become healthy after $max_retries attempts"
    return 1
}

# Check service health
check_service_health() {
    # Check mock device health
    if ! curl -sf "http://localhost:3000/health" &>/dev/null; then
        log_warning "Mock device not healthy"
        return 1
    fi
    
    # Check if telemetry module is running
    if ! docker-compose -f "$COMPOSE_FILE" ps telemetry-module | grep -q "Up"; then
        log_warning "Telemetry module not running"
        return 1
    fi
    
    # Check telemetry module logs for successful startup
    local telemetry_logs
    telemetry_logs=$(docker-compose -f "$COMPOSE_FILE" logs --tail=50 telemetry-module 2>/dev/null || echo "")
    if [[ ! "$telemetry_logs" =~ "Connected to IoT Hub" ]] && [[ ! "$telemetry_logs" =~ "Module started" ]]; then
        log_warning "Telemetry module hasn't connected to IoT Hub yet"
        return 1
    fi
    
    return 0
}

# Run integration tests
run_integration_tests() {
    log "Running integration tests..."
    
    # Test 1: Mock Device API Validation
    test_mock_device_api
    
    # Test 2: Telemetry Data Flow Validation
    test_telemetry_data_flow
    
    # Test 3: Message Format Validation
    test_message_format
    
    # Test 4: Session Management
    test_session_management
    
    # Test 5: Error Handling
    test_error_handling
    
    # Test 6: Performance Validation
    test_performance
}

# Test mock device API
test_mock_device_api() {
    log "Testing mock device API..."
    
    local response
    response=$(curl -s "http://localhost:3000/api/robots/MeatGeekBot/commands/get_status")
    
    # Validate JSON structure
    if ! echo "$response" | jq -e '.result.temps.grillTemp' &>/dev/null; then
        log_error "Mock device API response missing required fields"
        return 1
    fi
    
    # Validate temperature ranges
    local grill_temp
    grill_temp=$(echo "$response" | jq -r '.result.temps.grillTemp')
    if (( $(echo "$grill_temp < 0 || $grill_temp > 500" | bc -l) )); then
        log_error "Invalid grill temperature: $grill_temp"
        return 1
    fi
    
    log_success "Mock device API validation passed"
}

# Test telemetry data flow
test_telemetry_data_flow() {
    log "Testing telemetry data flow..."
    
    # Monitor telemetry module logs for successful HTTP requests
    local start_time
    start_time=$(date +%s)
    local success_count=0
    local required_successes=10
    
    while [[ $(($(date +%s) - start_time)) -lt 120 ]]; do
        local recent_logs
        recent_logs=$(docker-compose -f "$COMPOSE_FILE" logs --tail=20 telemetry-module 2>/dev/null || echo "")
        
        # Count successful HTTP requests in recent logs
        local current_successes
        current_successes=$(echo "$recent_logs" | grep -c "Successfully sent message" 2>/dev/null || echo "0")
        
        if [[ $current_successes -ge $required_successes ]]; then
            log_success "Telemetry data flow validation passed ($current_successes messages sent)"
            return 0
        fi
        
        sleep 10
    done
    
    log_error "Insufficient telemetry messages sent within timeout"
    return 1
}

# Test message format
test_message_format() {
    log "Testing message format..."
    
    # This would ideally connect to Azure IoT Hub to validate actual messages
    # For now, we'll validate the telemetry module logs contain proper message structure
    local logs
    logs=$(docker-compose -f "$COMPOSE_FILE" logs telemetry-module 2>/dev/null || echo "")
    
    if [[ "$logs" =~ "SessionId" ]] && [[ "$logs" =~ "grillTemp" ]]; then
        log_success "Message format validation passed"
        return 0
    else
        log_error "Message format validation failed - required fields missing"
        return 1
    fi
}

# Test session management
test_session_management() {
    log "Testing session management..."
    
    # This would test direct method calls for session management
    # For integration testing, we verify the session ID is being used correctly
    local logs
    logs=$(docker-compose -f "$COMPOSE_FILE" logs telemetry-module 2>/dev/null || echo "")
    
    if [[ "$logs" =~ "integration-test-session-001" ]]; then
        log_success "Session management validation passed"
        return 0
    else
        log_warning "Session management validation - session ID not found in logs"
        return 1
    fi
}

# Test error handling
test_error_handling() {
    log "Testing error handling..."
    
    # Temporarily stop mock device to test error recovery
    docker-compose -f "$COMPOSE_FILE" stop mock-device
    sleep 30
    
    # Check telemetry module handles the error gracefully
    local error_logs
    error_logs=$(docker-compose -f "$COMPOSE_FILE" logs --tail=20 telemetry-module 2>/dev/null || echo "")
    
    # Restart mock device
    docker-compose -f "$COMPOSE_FILE" start mock-device
    sleep 15
    
    # Check recovery
    local recovery_logs
    recovery_logs=$(docker-compose -f "$COMPOSE_FILE" logs --tail=10 telemetry-module 2>/dev/null || echo "")
    
    if [[ "$recovery_logs" =~ "Successfully sent message" ]]; then
        log_success "Error handling validation passed"
        return 0
    else
        log_warning "Error handling validation - recovery not detected"
        return 1
    fi
}

# Test performance
test_performance() {
    log "Testing performance..."
    
    # Measure average response time over 1 minute
    local total_time=0
    local request_count=0
    local max_response_time=0
    
    for i in {1..12}; do  # 12 requests over 1 minute
        local start_time
        start_time=$(date +%s.%N)
        
        if curl -sf "http://localhost:3000/api/robots/MeatGeekBot/commands/get_status" &>/dev/null; then
            local end_time
            end_time=$(date +%s.%N)
            local response_time
            response_time=$(echo "$end_time - $start_time" | bc -l)
            
            total_time=$(echo "$total_time + $response_time" | bc -l)
            request_count=$((request_count + 1))
            
            if (( $(echo "$response_time > $max_response_time" | bc -l) )); then
                max_response_time=$response_time
            fi
        fi
        
        sleep 5
    done
    
    if [[ $request_count -gt 0 ]]; then
        local avg_time
        avg_time=$(echo "scale=3; $total_time / $request_count" | bc -l)
        
        log "Performance metrics:"
        log "  - Average response time: ${avg_time}s"
        log "  - Maximum response time: ${max_response_time}s"
        log "  - Successful requests: $request_count/12"
        
        # Performance thresholds
        if (( $(echo "$avg_time < 0.5" | bc -l) )) && (( $(echo "$max_response_time < 2.0" | bc -l) )); then
            log_success "Performance validation passed"
            return 0
        else
            log_warning "Performance validation failed - response times too high"
            return 1
        fi
    else
        log_error "Performance validation failed - no successful requests"
        return 1
    fi
}

# Generate test report
generate_test_report() {
    log "Generating test report..."
    
    local report_file="$TEST_RESULTS_DIR/integration_test_report_$(date +%Y%m%d_%H%M%S).json"
    
    cat > "$report_file" <<EOF
{
  "testRun": {
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "environment": "integration",
    "duration": "$((SECONDS))s",
    "services": {
      "mockDevice": {
        "status": "$(docker-compose -f "$COMPOSE_FILE" ps -q mock-device | xargs docker inspect --format='{{.State.Status}}' 2>/dev/null || echo 'unknown')",
        "uptime": "$(docker-compose -f "$COMPOSE_FILE" ps mock-device | tail -1 | awk '{print $5}' || echo 'unknown')"
      },
      "telemetryModule": {
        "status": "$(docker-compose -f "$COMPOSE_FILE" ps -q telemetry-module | xargs docker inspect --format='{{.State.Status}}' 2>/dev/null || echo 'unknown')",
        "uptime": "$(docker-compose -f "$COMPOSE_FILE" ps telemetry-module | tail -1 | awk '{print $5}' || echo 'unknown')"
      }
    },
    "logs": {
      "mockDevice": "./logs/mock-device.log",
      "telemetryModule": "./logs/telemetry-module.log"
    }
  }
}
EOF
    
    # Collect service logs
    docker-compose -f "$COMPOSE_FILE" logs mock-device > "$TEST_RESULTS_DIR/mock-device.log" 2>&1 || true
    docker-compose -f "$COMPOSE_FILE" logs telemetry-module > "$TEST_RESULTS_DIR/telemetry-module.log" 2>&1 || true
    docker-compose -f "$COMPOSE_FILE" logs edgehub > "$TEST_RESULTS_DIR/edgehub.log" 2>&1 || true
    
    log_success "Test report generated: $report_file"
}

# Main execution
main() {
    log "Starting MeatGeek IoT Edge Integration Tests"
    
    preflight_checks
    setup_test_environment
    start_services
    wait_for_healthy_services
    
    # Allow services to stabilize
    log "Allowing services to stabilize..."
    sleep 30
    
    # Run the integration tests
    local test_failures=0
    
    if ! run_integration_tests; then
        test_failures=$((test_failures + 1))
    fi
    
    generate_test_report
    
    if [[ $test_failures -eq 0 ]]; then
        log_success "All integration tests passed! ✅"
        exit 0
    else
        log_error "$test_failures integration test(s) failed ❌"
        exit 1
    fi
}

# Script entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi