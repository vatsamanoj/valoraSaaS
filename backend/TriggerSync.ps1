$url = "http://localhost:5028/api/finance/sync-gl"
$headers = @{
    "Content-Type"  = "application/json"
    "X-Tenant-Id"   = "TEST_TENANT"
    "X-User-Id"     = "test-user"
    "X-Role"        = "TenantAdmin"
}

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers
    Write-Host "Sync Result: $response" -ForegroundColor Green
}
catch {
    Write-Host "Sync Failed: $($_.Exception.Message)" -ForegroundColor Red
}
