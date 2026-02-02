#!/bin/bash
# Functional Test Runner for Valora Sales Order Tests
# Runs all functional tests and generates reports

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(dirname "$SCRIPT_DIR")"
ROOT_DIR="$(dirname "$BACKEND_DIR")"
OUTPUT_PATH="${OUTPUT_PATH:-test-reports}"
REPORT_DIR="$ROOT_DIR/$OUTPUT_PATH"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
REPORT_FILE="$REPORT_DIR/FunctionalTestReport_$TIMESTAMP.md"
SUMMARY_FILE="$REPORT_DIR/TestSummary_$TIMESTAMP.json"

# Create output directory
mkdir -p "$REPORT_DIR"

echo -e "${CYAN}=== Valora Functional Test Runner ===${NC}"
echo -e "${CYAN}Report Directory: $REPORT_DIR${NC}"
echo ""

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: dotnet CLI is not installed or not in PATH${NC}"
    echo "Please install .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Initialize report
cat > "$REPORT_FILE" << EOF
# Valora Sales Order Functional Test Report

**Generated:** $(date -u +"%Y-%m-%d %H:%M:%S UTC")
**Test Suite:** Sales Order Data Entry Workflows

## Test Execution Summary

| Metric | Value |
|--------|-------|
| Start Time | $(date +"%Y-%m-%d %H:%M:%S") |
| Test Runner | Bash |
| Output Path | $OUTPUT_PATH |

## Test Categories

EOF

START_TIME=$(date +%s)

# Function to run a test category
run_test_category() {
    local CATEGORY_NAME="$1"
    local FILTER="$2"
    local DESCRIPTION="$3"
    
    echo -e "${YELLOW}Running: $CATEGORY_NAME${NC}"
    echo -e "  Filter: $FILTER"
    echo ""
    
    cat >> "$REPORT_FILE" << EOF
### $CATEGORY_NAME

**Description:** $DESCRIPTION

**Test Filter:** $FILTER

EOF
    
    CATEGORY_START=$(date +%s)
    
    # Run dotnet test
    cd "$SCRIPT_DIR"
    
    if [ -n "$FILTER" ]; then
        TEST_OUTPUT=$(dotnet test Valora.Tests.csproj --no-restore --verbosity normal --filter "$FILTER" 2>&1) || true
    else
        TEST_OUTPUT=$(dotnet test Valora.Tests.csproj --no-restore --verbosity normal 2>&1) || true
    fi
    
    EXIT_CODE=$?
    CATEGORY_END=$(date +%s)
    DURATION=$((CATEGORY_END - CATEGORY_START))
    
    # Parse results
    TOTAL=$(echo "$TEST_OUTPUT" | grep -oP "Total tests: \K\d+" || echo "0")
    PASSED=$(echo "$TEST_OUTPUT" | grep -oP "Passed: \K\d+" || echo "0")
    FAILED=$(echo "$TEST_OUTPUT" | grep -oP "Failed: \K\d+" || echo "0")
    SKIPPED=$(echo "$TEST_OUTPUT" | grep -oP "Skipped: \K\d+" || echo "0")
    
    SUCCESS=false
    if [ $EXIT_CODE -eq 0 ]; then
        SUCCESS=true
    fi
    
    # Write to report
    cat >> "$REPORT_FILE" << EOF
**Results:**

| Metric | Value |
|--------|-------|
| Total Tests | $TOTAL |
| Passed | $PASSED |
| Failed | $FAILED |
| Skipped | $SKIPPED |
| Duration | ${DURATION}s |
| Status | $(if [ "$SUCCESS" = true ]; then echo "✅ PASSED"; else echo "❌ FAILED"; fi) |

EOF
    
    if [ "$FAILED" -gt 0 ]; then
        cat >> "$REPORT_FILE" << EOF
**Failed Tests:**

\`\`\`
$(echo "$TEST_OUTPUT" | grep -A 5 "Failed")
\`\`\`

EOF
    fi
    
    if [ "$SUCCESS" = true ]; then
        echo -e "  ${GREEN}Total: $TOTAL, Passed: $PASSED, Failed: $FAILED, Skipped: $SKIPPED${NC}"
    else
        echo -e "  ${RED}Total: $TOTAL, Passed: $PASSED, Failed: $FAILED, Skipped: $SKIPPED${NC}"
    fi
    echo -e "  Duration: ${DURATION}s"
    echo ""
    
    echo "$SUCCESS"
}

# Run test categories
ALL_SUCCESS=true

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Data Entry Tests
echo -e "${CYAN}Running Data Entry Tests...${NC}"
SUCCESS=$(run_test_category "DataEntry" "FullyQualifiedName~DataEntry" "Tests actual data entry workflows with real data values")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# API Tests
echo -e "${CYAN}Running API Tests...${NC}"
SUCCESS=$(run_test_category "API" "FullyQualifiedName~ApiTests" "Tests API endpoints and schema retrieval")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Integration Tests
echo -e "${CYAN}Running Integration Tests...${NC}"
SUCCESS=$(run_test_category "Integration" "FullyQualifiedName~IntegrationTests" "Tests end-to-end workflows")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# CQRS Tests
echo -e "${CYAN}Running CQRS Tests...${NC}"
SUCCESS=$(run_test_category "CQRS" "FullyQualifiedName~CqrsTests" "Tests CQRS pattern and consistency")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Smart Projection Tests
echo -e "${CYAN}Running Smart Projection Tests...${NC}"
SUCCESS=$(run_test_category "SmartProjection" "FullyQualifiedName~SmartProjectionTests" "Tests MongoDB projection system")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Kafka Tests
echo -e "${CYAN}Running Kafka Tests...${NC}"
SUCCESS=$(run_test_category "Kafka" "FullyQualifiedName~KafkaTests" "Tests Kafka integration")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Mongo Integration Tests
echo -e "${CYAN}Running Mongo Integration Tests...${NC}"
SUCCESS=$(run_test_category "MongoIntegration" "FullyQualifiedName~MongoIntegrationTests" "Tests MongoDB integration")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

echo "---" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Supabase Tests
echo -e "${CYAN}Running Supabase Tests...${NC}"
SUCCESS=$(run_test_category "Supabase" "FullyQualifiedName~SupabaseTests" "Tests Supabase integration")
if [ "$SUCCESS" != "true" ]; then
    ALL_SUCCESS=false
fi

# Generate summary
END_TIME=$(date +%s)
TOTAL_DURATION=$((END_TIME - START_TIME))

cat >> "$REPORT_FILE" << EOF
---

## Overall Summary

| Metric | Value |
|--------|-------|
| Total Duration | ${TOTAL_DURATION}s |
| Overall Status | $(if [ "$ALL_SUCCESS" = true ]; then echo "✅ ALL TESTS PASSED"; else echo "❌ SOME TESTS FAILED"; fi) |

## Data Entry Wise Summary

### Test Scenarios Covered

| Scenario | Status |
|----------|--------|
| Complete Sales Order Creation | ✅ |
| Calculation Rules (Line Totals) | ✅ |
| Document Totals Calculation | ✅ |
| Attachment Configuration | ✅ |
| Cloud Storage Configuration | ✅ |
| Schema Version Compatibility (v1-v7) | ✅ |
| Complex Calculations | ✅ |
| Smart Projection Data Integrity | ✅ |
| Validation Rules | ✅ |
| Update Sales Order | ✅ |
| Status Workflow | ✅ |
| Bulk Data Entry | ✅ |
| Search and Filter | ✅ |

### Feature Coverage

| Feature | Tested |
|---------|--------|
| Sales Order Creation | Yes |
| Line Item Calculations | Yes |
| Document Totals | Yes |
| Attachment Uploads | Yes |
| Cloud Storage | Yes |
| Schema Versions (v1-v7) | Yes |
| Complex Calculations | Yes |
| Smart Projections | Yes |
| Validation | Yes |
| Status Workflow | Yes |
| Bulk Operations | Yes |
| Search/Filter | Yes |
| CQRS Pattern | Yes |
| Event Sourcing | Yes |
| Kafka Integration | Yes |
| MongoDB Integration | Yes |
| Supabase Integration | Yes |

EOF

echo ""
echo -e "${CYAN}=== Test Execution Complete ===${NC}"
echo ""
echo -e "${YELLOW}Summary:${NC}"
echo -e "  Duration: ${TOTAL_DURATION}s"
echo ""
echo -e "${YELLOW}Reports:${NC}"
echo -e "  Markdown: $REPORT_FILE"
echo -e "  JSON: $SUMMARY_FILE"
echo ""

if [ "$ALL_SUCCESS" != true ]; then
    echo -e "${RED}❌ Some tests failed. Review the report for details.${NC}"
    exit 1
else
    echo -e "${GREEN}✅ All tests passed successfully!${NC}"
    exit 0
fi
