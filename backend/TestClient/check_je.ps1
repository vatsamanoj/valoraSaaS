
$baseUrl = "http://localhost:5028"
$tenantId = "LAB_001"
$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $tenantId
    "X-User-Id" = "user-test"
}

Write-Host "Checking Journal Entries..."
try {
    $jeResponse = Invoke-RestMethod -Uri "$baseUrl/api/finance/journal-entries?PageSize=100" -Method Get -Headers $headers
    $entries = $jeResponse.Data.Items
    
    Write-Host "Found $($entries.Count) Journal Entries."

    $found = $entries | Where-Object { $_.Description -match "Billing for Order" -or $_.Description -match "Sales Order" }
    
    if ($found) {
        Write-Host "SUCCESS: Found Journal Entries for Sales Orders!"
        $found | Format-Table DocumentNumber, Description, TotalAmount, PostingDate | Out-String | Write-Host
    } else {
        Write-Host "WARNING: No Journal Entries found for Sales Orders yet."
        Write-Host "Recent Entries:"
        $entries | Select-Object -First 5 | Format-Table DocumentNumber, Description, TotalAmount | Out-String | Write-Host
    }

} catch {
    Write-Host "Error checking Journal Entries: $_"
}
