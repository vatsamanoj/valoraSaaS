
$baseUrl = "http://localhost:5028"
$tenantId = "LAB_001"
$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $tenantId
}

Write-Host "Checking Outbox Messages..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/debug/outbox" -Method Get -Headers $headers
    
    if ($response.Count -eq 0) {
        Write-Host "No Outbox Messages found."
    } else {
        Write-Host "Found $($response.Count) recent Outbox Messages."
        $response | Select-Object -First 5 | Format-Table Topic, Status, CreatedAt, Error | Out-String | Write-Host
        
        # Check specifically for valora.sd.so_billed
        $soEvents = $response | Where-Object { $_.Topic -eq "valora.sd.so_billed" }
        if ($soEvents) {
             Write-Host "FOUND SalesOrderBilledEvent!"
             $soEvents | Format-Table Topic, Status, CreatedAt, Error | Out-String | Write-Host
        } else {
             Write-Host "WARNING: No SalesOrderBilledEvent found in recent outbox."
        }
    }

} catch {
    Write-Host "Error checking outbox: $_"
}
