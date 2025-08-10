#!/bin/bash

# MeatGeek E2E Test CI/CD Runner
# Executes comprehensive E2E test suite in CI/CD environments

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
LOG_DIR="$PROJECT_DIR/logs"
RESULTS_DIR="$PROJECT_DIR/test-results"

# Test configuration
TIMEOUT_MINUTES=${E2E_TIMEOUT_MINUTES:-10}
PARALLEL_WORKERS=${E2E_PARALLEL_WORKERS:-2}
TEST_PATTERN=${E2E_TEST_PATTERN:-""}
ENABLE_COVERAGE=${E2E_COVERAGE:-false}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}ℹ️  [$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ [$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  [$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

log_error() {
    echo -e "${RED}❌ [$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

# Setup function
setup_test_environment() {
    log_info "Setting up E2E test environment..."
    
    # Create necessary directories
    mkdir -p "$LOG_DIR" "$RESULTS_DIR"
    
    # Check Node.js version
    local node_version=$(node --version)
    log_info "Node.js version: $node_version"
    
    # Check if required dependencies are available
    if ! npm list --depth=0 >/dev/null 2>&1; then
        log_info "Installing dependencies..."
        npm ci
    fi
    
    # Validate project structure
    local required_files=(
        "package.json"
        "jest.config.js"
        "utils/workflow-orchestrator.ts"
        "utils/mock-device-controller.ts"
        "utils/azure-client.ts"
    )
    
    for file in "${required_files[@]}"; do
        if [[ ! -f "$PROJECT_DIR/$file" ]]; then
            log_error "Required file not found: $file"
            exit 1
        fi
    done
    
    log_success "Test environment setup completed"
}

# Test execution function
run_test_suite() {
    local test_type="$1"
    local pattern="$2"
    local timeout="$3"
    
    log_info "Running $test_type tests..."
    
    local jest_args=(
        "--testTimeout=$((timeout * 60 * 1000))"
        "--maxWorkers=$PARALLEL_WORKERS"
        "--verbose"
        "--forceExit"
        "--detectOpenHandles"
    )
    
    if [[ "$ENABLE_COVERAGE" == "true" ]]; then
        jest_args+=(
            "--coverage"
            "--coverageDirectory=$RESULTS_DIR/coverage"
        )
    fi
    
    if [[ -n "$pattern" ]]; then
        jest_args+=("--testPathPattern=$pattern")
    fi
    
    # Execute tests with timeout and capture output
    local start_time=$(date +%s)
    local test_output_file="$LOG_DIR/${test_type}-$(date +%Y%m%d-%H%M%S).log"
    
    if timeout "${timeout}m" npm test -- "${jest_args[@]}" > "$test_output_file" 2>&1; then
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        log_success "$test_type tests completed in ${duration}s"
        return 0
    else
        local exit_code=$?
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        
        if [[ $exit_code -eq 124 ]]; then
            log_error "$test_type tests timed out after ${timeout}m"
        else
            log_error "$test_type tests failed (exit code: $exit_code) after ${duration}s"
        fi
        
        # Show last 50 lines of output for debugging
        log_warning "Last 50 lines of test output:"
        tail -n 50 "$test_output_file"
        
        return $exit_code
    fi
}

# Main test execution
run_e2e_tests() {
    log_info "Starting MeatGeek E2E Test Suite"
    log_info "Configuration:"
    log_info "  Timeout: ${TIMEOUT_MINUTES} minutes"
    log_info "  Workers: $PARALLEL_WORKERS"
    log_info "  Coverage: $ENABLE_COVERAGE"
    log_info "  Pattern: ${TEST_PATTERN:-'all tests'}"
    
    local overall_start_time=$(date +%s)
    local failed_suites=()
    
    # Test suites to run
    local test_suites=(
        "utils:test-data-factory.test.ts:2"
        "utils:mock-device-controller.simple.test.ts:3"
        "workflows:multi-scenario-validation.test.ts:5"
        "integration:cross-service-communication.test.ts:8"
    )
    
    # Add full workflow test if pattern allows (longer timeout)
    if [[ -z "$TEST_PATTERN" || "$TEST_PATTERN" == *"workflow"* ]]; then
        test_suites+=("workflows:brisket-cooking-journey.test.ts:15")
    fi
    
    # Add resilience tests if pattern allows
    if [[ -z "$TEST_PATTERN" || "$TEST_PATTERN" == *"resilience"* ]]; then
        test_suites+=("resilience:system-fault-tolerance.test.ts:10")
    fi
    
    # Execute each test suite
    for suite_config in "${test_suites[@]}"; do
        IFS=':' read -r suite_type suite_pattern suite_timeout <<< "$suite_config"
        
        # Skip if pattern doesn't match
        if [[ -n "$TEST_PATTERN" && "$suite_pattern" != *"$TEST_PATTERN"* ]]; then
            log_info "Skipping $suite_type/$suite_pattern (pattern mismatch)"
            continue
        fi
        
        if ! run_test_suite "$suite_type" "$suite_pattern" "$suite_timeout"; then
            failed_suites+=("$suite_type/$suite_pattern")
        fi
    done
    
    # Summary
    local overall_end_time=$(date +%s)
    local total_duration=$((overall_end_time - overall_start_time))
    
    if [[ ${#failed_suites[@]} -eq 0 ]]; then
        log_success "All E2E test suites passed! Total duration: ${total_duration}s"
        generate_success_report "$total_duration"
        return 0
    else
        log_error "Failed test suites: ${failed_suites[*]}"
        log_error "E2E tests failed after ${total_duration}s"
        generate_failure_report "${failed_suites[@]}" "$total_duration"
        return 1
    fi
}

# Report generation
generate_success_report() {
    local duration="$1"
    local report_file="$RESULTS_DIR/e2e-success-$(date +%Y%m%d-%H%M%S).json"
    
    cat > "$report_file" << EOF
{
  "status": "success",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "duration": $duration,
  "environment": {
    "node_version": "$(node --version)",
    "platform": "$(uname -s)",
    "ci": "${CI:-false}",
    "timeout_minutes": $TIMEOUT_MINUTES,
    "parallel_workers": $PARALLEL_WORKERS
  },
  "configuration": {
    "coverage_enabled": $ENABLE_COVERAGE,
    "test_pattern": "${TEST_PATTERN:-null}"
  }
}
EOF
    
    log_info "Success report generated: $report_file"
}

generate_failure_report() {
    local failed_suites=("$@")
    local duration="${failed_suites[-1]}"
    unset 'failed_suites[-1]'
    
    local report_file="$RESULTS_DIR/e2e-failure-$(date +%Y%m%d-%H%M%S).json"
    local failed_json=$(printf '"%s",' "${failed_suites[@]}")
    failed_json="[${failed_json%,}]"
    
    cat > "$report_file" << EOF
{
  "status": "failure",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "duration": $duration,
  "failed_suites": $failed_json,
  "environment": {
    "node_version": "$(node --version)",
    "platform": "$(uname -s)",
    "ci": "${CI:-false}",
    "timeout_minutes": $TIMEOUT_MINUTES,
    "parallel_workers": $PARALLEL_WORKERS
  },
  "configuration": {
    "coverage_enabled": $ENABLE_COVERAGE,
    "test_pattern": "${TEST_PATTERN:-null}"
  },
  "logs_directory": "$LOG_DIR"
}
EOF
    
    log_info "Failure report generated: $report_file"
}

# Cleanup function
cleanup() {
    log_info "Cleaning up test environment..."
    
    # Kill any background processes
    if pgrep -f "node.*jest" >/dev/null; then
        log_warning "Terminating remaining Jest processes..."
        pkill -f "node.*jest" || true
    fi
    
    # Archive logs if in CI
    if [[ "${CI:-false}" == "true" && -d "$LOG_DIR" ]]; then
        local archive_file="$RESULTS_DIR/e2e-logs-$(date +%Y%m%d-%H%M%S).tar.gz"
        tar -czf "$archive_file" -C "$PROJECT_DIR" "logs"
        log_info "Logs archived to: $archive_file"
    fi
    
    log_success "Cleanup completed"
}

# Signal handlers
trap cleanup EXIT
trap 'log_error "Test execution interrupted"; exit 130' INT TERM

# Main execution
main() {
    cd "$PROJECT_DIR"
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --timeout)
                TIMEOUT_MINUTES="$2"
                shift 2
                ;;
            --workers)
                PARALLEL_WORKERS="$2"
                shift 2
                ;;
            --pattern)
                TEST_PATTERN="$2"
                shift 2
                ;;
            --coverage)
                ENABLE_COVERAGE=true
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [options]"
                echo "Options:"
                echo "  --timeout MINUTES    Test timeout in minutes (default: 10)"
                echo "  --workers COUNT      Number of parallel workers (default: 2)"
                echo "  --pattern PATTERN    Test pattern to filter (optional)"
                echo "  --coverage           Enable code coverage collection"
                echo "  --help              Show this help message"
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                exit 1
                ;;
        esac
    done
    
    # Environment checks
    if [[ "${CI:-false}" == "true" ]]; then
        log_info "Running in CI environment"
    else
        log_info "Running in local environment"
    fi
    
    # Execute tests
    setup_test_environment
    run_e2e_tests
}

# Execute main function
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi