# Sales Order Template Extension - Test Runner Script
# This script runs all tests for the Sales Order screens

param(
    [string]$TestFilter = "",
    [switch]$Verbose,
    [switch]$MongoOnly,
    [switch]$ApiOnly,
    [switch]$IntegrationOnly,
    [switch]$SupabaseOnly,
    [switch]$KafkaOnly,
    [switch]$CqrsOnly,
    [switch]$InfrastructureOnly,
    [switch]$All
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendDir = Split-Path -Parent $scriptDir
$rootDir = Split-Path -Parent $backendDir

Write-Host "=== Valora Sales Order Test Runner ===" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is available
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found. Please install .NET SDK."
    exit 1
}

# Function to run tests with proper formatting
function Run-TestProject {
    param(
        [string]$ProjectPath,
        [string]$TestName
    )

    Write-Host "Running $TestName..." -ForegroundColor Yellow
    Write-Host "  Project: $ProjectPath" -ForegroundColor Gray

    $args = @("test", $ProjectPath, "--no-restore")

    if ($Verbose) {
        $args += "--verbosity", "detailed"
    }
    else {
        $args += "--verbosity", "normal"
    }

    if ($TestFilter) {
        $args += "--filter", $TestFilter
    }

    & dotnet @args

    if ($LASTEXITCODE -ne 0) {
        Write-Error "$TestName failed with exit code $LASTEXITCODE"
        return $false
    }

    Write-Host "  V $TestName passed" -ForegroundColor Green
    Write-Host ""
    return $true
}

# Function to run MongoDB verification
function Run-MongoVerification {
    Write-Host "Running MongoDB Verification..." -ForegroundColor Yellow

    $mongoVerifierPath = Join-Path $backendDir "MongoVerifier"

    if (!(Test-Path $mongoVerifierPath)) {
        Write-Warning "MongoVerifier project not found at $mongoVerifierPath"
        return $false
    }

    Push-Location $mongoVerifierPath
    try {
        & dotnet run --project MongoVerifier.csproj -- valora

        if ($LASTEXITCODE -ne 0) {
            Write-Error "MongoDB verification failed"
            return $false
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "  V MongoDB verification passed" -ForegroundColor Green
    Write-Host ""
    return $true
}

# Function to run frontend tests
function Run-FrontendTests {
    Write-Host "Running Frontend Tests..." -ForegroundColor Yellow

    $frontendDir = Join-Path $rootDir "frontend"

    if (!(Test-Path $frontendDir)) {
        Write-Warning "Frontend directory not found at $frontendDir"
        return $false
    }

    Push-Location $frontendDir
    try {
        # Check if npm/node is available
        if (!(Get-Command npm -ErrorAction SilentlyContinue)) {
            Write-Warning "npm not found. Skipping frontend tests."
            return $true  # Don't fail if npm is not installed
        }

        # Run vitest
        & npm test -- --run

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Frontend tests failed"
            return $false
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "  V Frontend tests passed" -ForegroundColor Green
    Write-Host ""
    return $true
}

# Main execution
$allPassed = $true

# Build test project first
Write-Host "Building test project..." -ForegroundColor Yellow
$testProjectPath = Join-Path $scriptDir "Valora.Tests.csproj"
& dotnet build $testProjectPath --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build test project"
    exit 1
}

Write-Host "  V Build successful" -ForegroundColor Green
Write-Host ""

# Run tests based on filters
if ($MongoOnly) {
    $allPassed = Run-MongoVerification
}
elseif ($SupabaseOnly) {
    $allPassed = Run-TestProject -ProjectPath $testProjectPath -TestName "Supabase/PostgreSQL CQRS Tests" -Filter "FullyQualifiedName~SupabaseTests"
}
elseif ($KafkaOnly) {
    $allPassed = Run-TestProject -ProjectPath $testProjectPath -TestName "Kafka Event Streaming Tests" -Filter "FullyQualifiedName~KafkaTests"
}
elseif ($CqrsOnly) {
    $allPassed = Run-TestProject -ProjectPath $testProjectPath -TestName "CQRS Pattern Tests" -Filter "FullyQualifiedName~CqrsTests"
}
elseif ($InfrastructureOnly) {
    # Run all infrastructure tests (Supabase, Kafka, MongoDB, CQRS)
    Write-Host "=== Running Infrastructure Tests ===" -ForegroundColor Cyan
    Write-Host ""

    if (!(Run-MongoVerification)) {
        $allPassed = $false
    }

    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "Supabase/PostgreSQL CQRS Tests" -Filter "FullyQualifiedName~SupabaseTests")) {
        $allPassed = $false
    }

    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "Kafka Event Streaming Tests" -Filter "FullyQualifiedName~KafkaTests")) {
        $allPassed = $false
    }

    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "MongoDB Integration Tests" -Filter "FullyQualifiedName~MongoIntegrationTests")) {
        $allPassed = $false
    }

    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "CQRS Pattern Tests" -Filter "FullyQualifiedName~CqrsTests")) {
        $allPassed = $false
    }
}
elseif ($ApiOnly) {
    $allPassed = Run-TestProject -ProjectPath $testProjectPath -TestName "API Tests" -Filter "FullyQualifiedName~ApiTests"
}
elseif ($IntegrationOnly) {
    $allPassed = Run-TestProject -ProjectPath $testProjectPath -TestName "Integration Tests" -Filter "FullyQualifiedName~IntegrationTests"
}
elseif ($All -or (!$MongoOnly -and !$SupabaseOnly -and !$KafkaOnly -and !$CqrsOnly -and !$InfrastructureOnly -and !$ApiOnly -and !$IntegrationOnly)) {
    # Run all tests (default behavior)
    Write-Host "=== Running All Tests ===" -ForegroundColor Cyan
    Write-Host ""

    # 1. MongoDB Verification
    if (!(Run-MongoVerification)) {
        $allPassed = $false
    }

    # 2. Supabase/PostgreSQL CQRS Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "Supabase/PostgreSQL CQRS Tests")) {
        $allPassed = $false
    }

    # 3. Kafka Event Streaming Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "Kafka Event Streaming Tests")) {
        $allPassed = $false
    }

    # 4. MongoDB Integration Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "MongoDB Integration Tests")) {
        $allPassed = $false
    }

    # 5. CQRS Pattern Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "CQRS Pattern Tests")) {
        $allPassed = $false
    }

    # 6. Backend API Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "API Tests")) {
        $allPassed = $false
    }

    # 7. Integration Tests
    if (!(Run-TestProject -ProjectPath $testProjectPath -TestName "Integration Tests")) {
        $allPassed = $false
    }

    # 8. Frontend Tests
    if (!(Run-FrontendTests)) {
        $allPassed = $false
    }
}

# Summary
Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan

if ($allPassed) {
    Write-Host "All tests passed! V" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "Some tests failed! X" -ForegroundColor Red
    exit 1
}
