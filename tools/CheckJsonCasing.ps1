$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = "TEST_TENANT"
}
$response = Invoke-WebRequest -Uri "http://localhost:5005/api/finance/journal-entries?page=1&pageSize=1" -Method Get -Headers $headers
Write-Host "Raw Content: $($response.Content)"
