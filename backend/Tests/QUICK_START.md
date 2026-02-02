# Quick Start - Running Functional Tests

## Prerequisites Check

```bash
# Check if .NET SDK is installed
dotnet --version

# If not installed, download from:
# https://dotnet.microsoft.com/download
```

## Run All Tests (Simplest Method)

```bash
cd backend/Tests
./run-all-tests.sh
```

Or using dotnet directly:

```bash
cd backend/Tests
dotnet test
```

## Run Specific Test Categories

### Data Entry Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~DataEntry"
```

### CQRS Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~CqrsTests"
```

### API Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~ApiTests"
```

### Integration Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Smart Projection Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~SmartProjectionTests"
```

### Kafka Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~KafkaTests"
```

### MongoDB Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~MongoIntegrationTests"
```

### Supabase Tests
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~SupabaseTests"
```

## Run with Detailed Reports

```bash
cd backend/Tests
./run-functional-tests.sh
```

This generates:
- Markdown report in `test-reports/FunctionalTestReport_<timestamp>.md`
- JSON summary in `test-reports/TestSummary_<timestamp>.json`

## Run Single Test

```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~<TestClassName>.<TestMethodName>"
```

Example:
```bash
dotnet test --filter "FullyQualifiedName~SalesOrderCqrsTests.CreateSalesOrderCommandHandler_ValidCommand_CreatesSalesOrder"
```

## Common Options

### Verbose Output
```bash
dotnet test --verbosity detailed
```

### No Build (faster if already built)
```bash
dotnet test --no-build
```

### Parallel Execution
```bash
dotnet test --parallel
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Troubleshooting

### .NET Not Found
```bash
# Install .NET SDK
# Linux/macOS: https://dotnet.microsoft.com/download
# Windows: Download installer from above link
```

### Permission Denied
```bash
chmod +x backend/Tests/run-all-tests.sh
chmod +x backend/Tests/run-functional-tests.sh
```

### Services Not Running
```bash
# Start MongoDB
docker run -d -p 27017:27017 mongo

# Start Kafka (requires docker-compose)
# See project docker-compose.yml
```

## Test Files Overview

| File | Description |
|------|-------------|
| [`SalesOrderCqrsTests.cs`](SalesOrderCqrsTests.cs) | CQRS pattern and consistency tests |
| [`SalesOrderDataEntryTests.cs`](SalesOrderDataEntryTests.cs) | Data entry workflow tests |
| [`SalesOrderApiTests.cs`](SalesOrderApiTests.cs) | API endpoint tests |
| [`SalesOrderIntegrationTests.cs`](SalesOrderIntegrationTests.cs) | End-to-end integration tests |
| [`SmartProjectionTests.cs`](SmartProjectionTests.cs) | MongoDB projection tests |
| [`SalesOrderKafkaTests.cs`](SalesOrderKafkaTests.cs) | Kafka integration tests |
| [`SalesOrderMongoIntegrationTests.cs`](SalesOrderMongoIntegrationTests.cs) | MongoDB integration tests |
| [`SalesOrderSupabaseTests.cs`](SalesOrderSupabaseTests.cs) | Supabase/PostgreSQL tests |

## Expected Results

When all tests pass, you should see:

```
Test run for Valora.Tests.csproj (.NET 8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.x.x

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   100+, Skipped:     0, Total:   100+, Duration: 3-7 min
```

## Next Steps

- Review [`FUNCTIONAL_TESTS_GUIDE.md`](FUNCTIONAL_TESTS_GUIDE.md) for detailed documentation
- Check test reports in `test-reports/` directory
- Add new tests following existing patterns
- Integrate with CI/CD pipeline

## Support

For detailed information, see:
- [`FUNCTIONAL_TESTS_GUIDE.md`](FUNCTIONAL_TESTS_GUIDE.md) - Complete testing guide
- [`README.md`](README.md) - Test suite overview
- Project root `README.md` - Overall project documentation
