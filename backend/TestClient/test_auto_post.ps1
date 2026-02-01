
$baseUrl = "http://localhost:5028"
$tenantId = "LAB003"
$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $tenantId
    "X-User-Id" = "user-test"
}

# 1. Create Material
$materialCode = "MAT-AUTO-TEST-$(Get-Random)"
$materialBody = @{
    MaterialCode = $materialCode
    Description = "Auto Test Material"
    BaseUnitOfMeasure = "PCS"
    StandardPrice = 100.00
} | ConvertTo-Json

Write-Host "Creating Material: $materialCode..."
try {
    $matResponse = Invoke-RestMethod -Uri "$baseUrl/api/materials/materials" -Method Post -Headers $headers -Body $materialBody
    Write-Host "Material Created: $($matResponse.Success)"
} catch {
    Write-Host "Error creating material: $_"
    # It might already exist, which is fine
}

# 2. Create GL Account (Customer)
$customerName = "Revenue-Auto-Test"
$glBody = @{
    AccountCode = "REV-$(Get-Random)"
    Name = $customerName
    Type = "Revenue"
} | ConvertTo-Json

Write-Host "Creating GL Account: $customerName..."
try {
    $glResponse = Invoke-RestMethod -Uri "$baseUrl/api/finance/gl-accounts" -Method Post -Headers $headers -Body $glBody
    Write-Host "GL Account Created: $($glResponse.Success)"
} catch {
    Write-Host "Error creating GL Account: $_"
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            Write-Host "Response Body: $body"
        }
    } catch {}
}

# 3. Create Sales Order (Generic API)
$soBody = @{
    OrderNumber = "SO-AUTO-$(Get-Random)"
    OrderDate = [DateTime]::UtcNow.ToString("o")
    CustomerId = $customerName
    Currency = "USD"
    Status = "Draft" # Should become Invoiced if AutoPost works
    Items = @(
        @{
            MaterialCode = $materialCode
            Quantity = 2
            UnitPrice = 100.00
        }
    )
    TotalAmount = 200.00
} | ConvertTo-Json

Write-Host "Creating Sales Order via Generic API..."
try {
    $soResponse = Invoke-RestMethod -Uri "$baseUrl/api/data/SalesOrder" -Method Post -Headers $headers -Body $soBody
    Write-Host "Sales Order Response: $($soResponse.Success)"
    Write-Host $soResponse
} catch {
    Write-Host "Error creating Sales Order: $_"
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            Write-Host "Response Body: $body"
        }
    } catch {
        Write-Host "Could not read error body."
    }
    exit
}

# 4. Wait for Eventual Consistency
Write-Host "Waiting for Auto-Post (15s)..."
Start-Sleep -Seconds 15

# 5. Check Journal Entries
Write-Host "Checking Journal Entries..."
try {
    $jeResponse = Invoke-RestMethod -Uri "$baseUrl/api/finance/journal-entries" -Method Get -Headers $headers
    $entries = $jeResponse.Data.Items
    
    Write-Host "Found $($entries.Count) Journal Entries."

    # Filter for our SO (FiIntegrationService uses "Billing for Order {OrderNumber}")
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

Write-Host "Script Finished."
