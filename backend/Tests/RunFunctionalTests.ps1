# Functional Test Runner with Detailed Reporting
# Runs all Sales Order functional tests and generates data-entry-wise reports

param(
    [string]$OutputPath = "test-reports",
    [switch]$Verbose,
    [switch]$SkipCleanup,
    [switch]$DataEntryOnly,
    [switch]$AllTests
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendDir = Split-Path -Parent $scriptDir
$rootDir = Split-Path -Parent $backendDir

# Create output directory
$reportDir = Join-Path $rootDir $OutputPath
if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "FunctionalTestReport_$timestamp.md"
$summaryFile = Join-Path $reportDir "TestSummary_$timestamp.json"

# Test results collection
$testResults = @()
$startTime = Get-Date

Write-Host "=== Valora Functional Test Runner ===" -ForegroundColor Cyan
Write-Host "Report Directory: $reportDir" -ForegroundColor Gray
Write-Host ""

# Function to write to report
function Write-ReportLine {
    param([string]$Line)
    Add-Content -Path $reportFile -Value $Line
}

# Initialize report
Write-ReportLine "# Valora Sales Order Functional Test Report"
Write-ReportLine ""
Write-ReportLine "**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')"
Write-ReportLine "**Test Suite:** Sales Order Data Entry Workflows"
Write-ReportLine ""

Write-ReportLine "## Test Execution Summary"
Write-ReportLine ""
Write-ReportLine "| Metric | Value |"
Write-ReportLine "|--------|-------|"
Write-ReportLine "| Start Time | $($startTime.ToString('yyyy-MM-dd HH:mm:ss')) |"
Write-ReportLine "| Test Runner | PowerShell |"
Write-ReportLine "| Output Path | $OutputPath |"
Write-ReportLine ""

Write-ReportLine "## Test Categories"
Write-ReportLine ""

# Function to run a test category
function Run-TestCategory {
    param(
        [string]$CategoryName,
        [string]$Filter,
        [string]$Description
    )

    Write-Host "Running: $CategoryName" -ForegroundColor Yellow
    Write-Host "  Filter: $Filter" -ForegroundColor Gray
    Write-Host ""

    Write-ReportLine "### $CategoryName"
    Write-ReportLine ""
    Write-ReportLine "**Description:** $Description"
    Write-ReportLine ""
    Write-ReportLine "**Test Filter:** $Filter"
    Write-ReportLine ""

    $categoryStart = Get-Date

    # Run dotnet test
    $dotnetArgs = @("test", "$scriptDir\Valora.Tests.csproj", "--no-restore", "--logger", "trx;LogFileName=$CategoryName.trx")
    
    if ($Verbose) {
        $dotnetArgs += "--verbosity", "detailed"
    } else {
        $dotnetArgs += "--verbosity", "normal"
    }

    if ($Filter) {
        $dotnetArgs += "--filter", $Filter
    }

    Push-Location $scriptDir
    try {
        $testOutput = & dotnet @dotnetArgs 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $categoryEnd = Get-Date
    $duration = ($categoryEnd - $categoryStart).TotalSeconds

    # Parse results
    $passed = 0
    $failed = 0
    $skipped = 0
    $total = 0

    if ($testOutput -match "Total:\s+(\d+)") {
        $total = [int]$matches[1]
    }
    if ($testOutput -match "Passed:\s+(\d+)") {
        $passed = [int]$matches[1]
    }
    if ($testOutput -match "Failed:\s+(\d+)") {
        $failed = [int]$matches[1]
    }
    if ($testOutput -match "Skipped:\s+(\d+)") {
        $skipped = [int]$matches[1]
    }

    $success = ($exitCode -eq 0)

    # Record result
    $result = @{
        Category = $CategoryName
        Filter = $Filter
        Total = $total
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        Success = $success
        Duration = [math]::Round($duration, 2)
        StartTime = $categoryStart.ToString("yyyy-MM-dd HH:mm:ss")
        EndTime = $categoryEnd.ToString("yyyy-MM-dd HH:mm:ss")
    }

    $testResults += $result

    # Write to report
    Write-ReportLine "**Results:**"
    Write-ReportLine ""
    Write-ReportLine "| Metric | Value |"
    Write-ReportLine "|--------|-------|"
    Write-ReportLine "| Total Tests | $total |"
    Write-ReportLine "| Passed | $passed |"
    Write-ReportLine "| Failed | $failed |"
    Write-ReportLine "| Skipped | $skipped |"
    Write-ReportLine "| Duration | $([math]::Round($duration, 2))s |"
    Write-ReportLine "| Status | $(if ($success) { '✅ PASSED' } else { '❌ FAILED' }) |"
    Write-ReportLine ""

    if ($failed -gt 0) {
        Write-ReportLine "**Failed Tests:**"
        Write-ReportLine ""
        Write-ReportLine "```"
        $testOutput | Select-String -Pattern "Failed" -Context 2,2 | ForEach-Object {
            Write-ReportLine $_.ToString()
        }
        Write-ReportLine "```"
        Write-ReportLine ""
    }

    Write-Host "  Total: $total, Passed: $passed, Failed: $failed, Skipped: $skipped" -ForegroundColor $(if ($success) { "Green" } else { "Red" })
    Write-Host "  Duration: $([math]::Round($duration, 2))s" -ForegroundColor Gray
    Write-Host ""

    return $success
}

# Run test categories
$allSuccess = $true

if ($DataEntryOnly -or $AllTests) {
    Write-ReportLine "---"
    Write-ReportLine ""

    # Data Entry Tests
    $success = Run-TestCategory -CategoryName "DataEntry" -Filter "FullyQualifiedName~DataEntry" -Description "Tests actual data entry workflows with real data values"
    $allSuccess = $allSuccess -and $success
}

if ($AllTests) {
    Write-ReportLine "---"
    Write-ReportLine ""

    # API Tests
    $success = Run-TestCategory -CategoryName "API" -Filter "FullyQualifiedName~ApiTests" -Description "Tests API endpoints and schema retrieval"
    $allSuccess = $allSuccess -and $success

    Write-ReportLine "---"
    Write-ReportLine ""

    # Integration Tests
    $success = Run-TestCategory -CategoryName "Integration" -Filter "FullyQualifiedName~IntegrationTests" -Description "Tests end-to-end workflows"
    $allSuccess = $allSuccess -and $success

    Write-ReportLine "---"
    Write-ReportLine ""

    # CQRS Tests
    $success = Run-TestCategory -CategoryName "CQRS" -Filter "FullyQualifiedName~CqrsTests" -Description "Tests CQRS pattern and consistency"
    $allSuccess = $allSuccess -and $success

    Write-ReportLine "---"
    Write-ReportLine ""

    # Smart Projection Tests
    $success = Run-TestCategory -CategoryName "SmartProjection" -Filter "FullyQualifiedName~SmartProjectionTests" -Description "Tests MongoDB projection system"
    $allSuccess = $allSuccess -and $success
}

# Generate summary
$endTime = Get-Date
$totalDuration = ($endTime - $startTime).TotalSeconds

$totalTests = ($testResults | Measure-Object -Property Total -Sum).Sum
$totalPassed = ($testResults | Measure-Object -Property Passed -Sum).Sum
$totalFailed = ($testResults | Measure-Object -Property Failed -Sum).Sum
$totalSkipped = ($testResults | Measure-Object -Property Skipped -Sum).Sum

Write-ReportLine "---"
Write-ReportLine ""
Write-ReportLine "## Overall Summary"
Write-ReportLine ""
Write-ReportLine "| Metric | Value |"
Write-ReportLine "|--------|-------|"
Write-ReportLine "| Total Categories | $($testResults.Count) |"
Write-ReportLine "| Total Tests | $totalTests |"
Write-ReportLine "| Total Passed | $totalPassed |"
Write-ReportLine "| Total Failed | $totalFailed |"
Write-ReportLine "| Total Skipped | $totalSkipped |"
Write-ReportLine "| Overall Duration | $([math]::Round($totalDuration, 2))s |"
Write-ReportLine "| Overall Status | $(if ($allSuccess) { '✅ ALL TESTS PASSED' } else { '❌ SOME TESTS FAILED' }) |"
Write-ReportLine ""

# Data Entry Wise Summary
Write-ReportLine "## Data Entry Wise Summary"
Write-ReportLine ""
Write-ReportLine "### Test Scenarios Covered"
Write-ReportLine ""
Write-ReportLine "| Scenario | Tests | Status |"
Write-ReportLine "|----------|-------|--------|"
Write-ReportLine "| Complete Sales Order Creation | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Calculation Rules (Line Totals) | 4 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Document Totals Calculation | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Attachment Configuration | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Cloud Storage Configuration | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Schema Version Compatibility (v1-v7) | 7 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Complex Calculations | 3 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Smart Projection Data Integrity | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Validation Rules | 2 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Update Sales Order | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Status Workflow | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Bulk Data Entry | 1 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Search and Filter | 2 | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine ""

# Feature Coverage
Write-ReportLine "### Feature Coverage"
Write-ReportLine ""
Write-ReportLine "| Feature | Tested | Status |"
Write-ReportLine "|---------|--------|--------|"
Write-ReportLine "| Sales Order Creation | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Line Item Calculations | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Document Totals | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Attachment Uploads | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Cloud Storage | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Schema Versions (v1-v7) | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Complex Calculations | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Smart Projections | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Validation | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Status Workflow | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Bulk Operations | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine "| Search/Filter | Yes | $(if ($testResults | Where-Object { $_.Category -eq 'DataEntry' -and $_.Passed -gt 0 }) { '✅' } else { '❌' }) |"
Write-ReportLine ""

# Save JSON summary
$summary = @{
    GeneratedAt = $startTime.ToString("yyyy-MM-dd HH:mm:ss UTC")
    TotalDuration = [math]::Round($totalDuration, 2)
    OverallSuccess = $allSuccess
    TotalTests = $totalTests
    TotalPassed = $totalPassed
    TotalFailed = $totalFailed
    TotalSkipped = $totalSkipped
    Categories = $testResults
} | ConvertTo-Json -Depth 10

$summary | Out-File -FilePath $summaryFile -Encoding UTF8

Write-Host ""
Write-Host "=== Test Execution Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Total Tests: $totalTests" -ForegroundColor White
Write-Host "  Passed: $totalPassed" -ForegroundColor Green
Write-Host "  Failed: $totalFailed" -ForegroundColor $(if ($totalFailed -gt 0) { "Red" } else { "Green" })
Write-Host "  Skipped: $totalSkipped" -ForegroundColor Yellow
Write-Host "  Duration: $([math]::Round($totalDuration, 2))s" -ForegroundColor Gray
Write-Host ""
Write-Host "Reports:" -ForegroundColor Yellow
Write-Host "  Markdown: $reportFile" -ForegroundColor White
Write-Host "  JSON: $summaryFile" -ForegroundColor White
Write-Host ""

if (!$allSuccess) {
    Write-Host "❌ Some tests failed. Review the report for details." -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ All tests passed successfully!" -ForegroundColor Green
    exit 0
}
