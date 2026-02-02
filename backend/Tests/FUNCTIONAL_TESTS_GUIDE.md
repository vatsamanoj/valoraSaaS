# Valora Functional Tests Guide

## Overview

This guide provides comprehensive information about running functional tests for the Valora Sales Order system. The test suite covers all aspects of the CQRS architecture, data entry workflows, and integration points.

## Prerequisites

### Required Software
- **.NET SDK 8.0 or later** - [Download](https://dotnet.microsoft.com/download)
- **PowerShell Core** (optional, for Windows-style scripts) - [Download](https://github.com/PowerShell/PowerShell)
- **Bash** (for Linux/macOS scripts)

### Required Services (for integration tests)
- **MongoDB** - For projection and read model tests
- **Kafka** - For event streaming tests
- **Supabase/PostgreSQL** - For write model tests

## Test Categories

### 1. Data Entry Tests (`SalesOrderDataEntryTests.cs`)
Tests actual data entry workflows with real data values.

**Test Cases:**
- Complete Sales Order Creation
- Calculation Rules (Line Totals)
- Document Totals Calculation
- Attachment Configuration
- Cloud Storage Configuration
- Schema Version Compatibility (v1-v7)
- Complex Calculations
- Smart Projection Data Integrity
- Validation Rules
- Update Sales Order
- Status Workflow
- Bulk Data Entry
- Search and Filter

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~DataEntry"
```

### 2. API Tests (`SalesOrderApiTests.cs`)
Tests API endpoints and schema retrieval.

**Test Cases:**
- GET /api/sales/orders
- POST /api/sales/orders
- PUT /api/sales/orders/{id}
- DELETE /api/sales/orders/{id}
- Schema endpoint validation
- Error handling
- Authentication/Authorization

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~ApiTests"
```

### 3. Integration Tests (`SalesOrderIntegrationTests.cs`)
Tests end-to-end workflows across multiple components.

**Test Cases:**
- Complete order lifecycle
- Cross-module integration
- Transaction consistency
- Error recovery
- Concurrent operations

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### 4. CQRS Tests (`SalesOrderCqrsTests.cs`)
Tests CQRS pattern implementation and consistency.

**Test Cases:**
- Command handler execution
- Event sourcing verification
- Projection updates
- Read/Write model consistency
- Outbox pattern verification
- Event replay
- Snapshot creation

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~CqrsTests"
```

### 5. Smart Projection Tests (`SmartProjectionTests.cs`)
Tests MongoDB projection system and data transformation.

**Test Cases:**
- Projection creation
- Field mapping
- Nested object handling
- Array transformations
- Calculated fields
- Index management
- Query optimization

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~SmartProjectionTests"
```

### 6. Kafka Tests (`SalesOrderKafkaTests.cs`)
Tests Kafka integration and event streaming.

**Test Cases:**
- Event publishing
- Event consumption
- Topic management
- Partition handling
- Consumer groups
- Error handling
- Retry logic

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~KafkaTests"
```

### 7. MongoDB Integration Tests (`SalesOrderMongoIntegrationTests.cs`)
Tests MongoDB integration and data persistence.

**Test Cases:**
- Document creation
- Document updates
- Query operations
- Aggregation pipelines
- Index usage
- Transaction support
- Connection pooling

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~MongoIntegrationTests"
```

### 8. Supabase Tests (`SalesOrderSupabaseTests.cs`)
Tests Supabase/PostgreSQL integration.

**Test Cases:**
- Write model persistence
- Transaction management
- Constraint validation
- Foreign key relationships
- Trigger execution
- RLS policies

**Run Command:**
```bash
dotnet test --filter "FullyQualifiedName~SupabaseTests"
```

## Running Tests

### Option 1: Run All Tests (Bash Script)

```bash
cd backend/Tests
./run-functional-tests.sh
```

This will:
- Run all test categories sequentially
- Generate a detailed markdown report
- Create a JSON summary
- Display results in the console

### Option 2: Run All Tests (PowerShell Script)

```powershell
cd backend/Tests
.\RunFunctionalTests.ps1 -AllTests
```

Options:
- `-AllTests` - Run all test categories
- `-DataEntryOnly` - Run only data entry tests
- `-Verbose` - Show detailed output
- `-SkipCleanup` - Keep test data after execution

### Option 3: Run Specific Test Category

```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~<CategoryName>"
```

Replace `<CategoryName>` with:
- `DataEntry`
- `ApiTests`
- `IntegrationTests`
- `CqrsTests`
- `SmartProjectionTests`
- `KafkaTests`
- `MongoIntegrationTests`
- `SupabaseTests`

### Option 4: Run Single Test

```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~<TestClassName>.<TestMethodName>"
```

Example:
```bash
dotnet test --filter "FullyQualifiedName~SalesOrderCqrsTests.CreateSalesOrderCommandHandler_ValidCommand_CreatesSalesOrder"
```

### Option 5: Run All Tests (Simple)

```bash
cd backend/Tests
dotnet test
```

## Test Reports

### Report Location
Reports are generated in the `test-reports/` directory at the project root.

### Report Types

1. **Markdown Report** (`FunctionalTestReport_<timestamp>.md`)
   - Detailed test execution summary
   - Category-wise results
   - Failed test details
   - Feature coverage matrix
   - Data entry scenario coverage

2. **JSON Summary** (`TestSummary_<timestamp>.json`)
   - Machine-readable test results
   - Useful for CI/CD integration
   - Contains timing and status information

3. **TRX Files** (Visual Studio Test Results)
   - Generated per test category
   - Can be viewed in Visual Studio
   - Located in `TestResults/` directory

## Environment Configuration

### Required Environment Variables

```bash
# Database Connections
export SUPABASE_URL="your-supabase-url"
export SUPABASE_KEY="your-supabase-key"
export MONGODB_CONNECTION="mongodb://localhost:27017"
export KAFKA_BOOTSTRAP_SERVERS="localhost:9092"

# Test Configuration
export TEST_TENANT_ID="test-tenant"
export TEST_USER_ID="test-user"
```

### Configuration Files

Tests use the following configuration files:
- `appsettings.json` - Main configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Test.json` - Test-specific settings (if exists)

## Continuous Integration

### GitHub Actions Example

```yaml
name: Functional Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      mongodb:
        image: mongo:latest
        ports:
          - 27017:27017
      
      kafka:
        image: confluentinc/cp-kafka:latest
        ports:
          - 9092:9092
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run functional tests
        run: |
          cd backend/Tests
          ./run-functional-tests.sh
      
      - name: Upload test reports
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-reports
          path: test-reports/
```

## Troubleshooting

### Common Issues

#### 1. .NET SDK Not Found
```
Error: dotnet: command not found
```

**Solution:** Install .NET SDK from https://dotnet.microsoft.com/download

#### 2. MongoDB Connection Failed
```
Error: Unable to connect to MongoDB
```

**Solution:** 
- Ensure MongoDB is running: `docker run -d -p 27017:27017 mongo`
- Check connection string in environment variables

#### 3. Kafka Connection Failed
```
Error: Unable to connect to Kafka
```

**Solution:**
- Ensure Kafka is running
- Check bootstrap servers configuration
- Verify network connectivity

#### 4. Test Timeout
```
Error: Test execution timed out
```

**Solution:**
- Increase timeout in test configuration
- Check if services are responding
- Review test logs for hanging operations

#### 5. Permission Denied (Bash Script)
```
Error: Permission denied: ./run-functional-tests.sh
```

**Solution:**
```bash
chmod +x backend/Tests/run-functional-tests.sh
```

## Test Data Management

### Test Data Cleanup

Tests automatically clean up data after execution. To skip cleanup:

```bash
# PowerShell
.\RunFunctionalTests.ps1 -SkipCleanup

# Bash (modify script or set environment variable)
export SKIP_CLEANUP=true
./run-functional-tests.sh
```

### Test Data Seeding

Some tests require seed data. Run the seeder before tests:

```bash
cd backend/SchemaSeeder
dotnet run
```

## Performance Benchmarks

Expected test execution times (approximate):

| Test Category | Duration | Test Count |
|--------------|----------|------------|
| Data Entry | 30-60s | 20+ |
| API Tests | 15-30s | 15+ |
| Integration | 45-90s | 10+ |
| CQRS | 30-60s | 15+ |
| Smart Projection | 20-40s | 12+ |
| Kafka | 25-50s | 10+ |
| MongoDB | 20-40s | 12+ |
| Supabase | 25-50s | 10+ |
| **Total** | **3-7 minutes** | **100+** |

## Best Practices

1. **Run tests locally before pushing** - Catch issues early
2. **Use filters for focused testing** - Speed up development
3. **Review test reports** - Understand failures
4. **Keep services running** - Faster test execution
5. **Clean test data regularly** - Avoid conflicts
6. **Update tests with features** - Maintain coverage
7. **Monitor test performance** - Identify slow tests
8. **Use parallel execution** - When possible

## Contributing

When adding new tests:

1. Follow existing naming conventions
2. Add appropriate test categories
3. Update this documentation
4. Ensure tests are idempotent
5. Clean up test data
6. Add meaningful assertions
7. Include test descriptions

## Support

For issues or questions:
- Check the troubleshooting section
- Review test logs in `test-reports/`
- Check service logs (MongoDB, Kafka, etc.)
- Consult the main README.md
- Contact the development team

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html)
