$baseUrl = "http://localhost:5028"
$headers = @{
    "X-Tenant-Id" = "TEST_TENANT"
    "Content-Type" = "application/json"
}
$suffix = Get-Random -Minimum 1000 -Maximum 9999

function Test-Endpoint {
    param($Method, $Uri, $Body)
    Write-Host "[$Method] $Uri" -ForegroundColor Cyan
    try {
        if ($Body) {
            $response = Invoke-RestMethod -Method $Method -Uri "$baseUrl$Uri" -Headers $headers -Body ($Body | ConvertTo-Json -Depth 5) -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Method $Method -Uri "$baseUrl$Uri" -Headers $headers -ErrorAction Stop
        }
        Write-Host "Success: $($response.success)" -ForegroundColor Green
        return $response
    } catch {
        Write-Host "Error: $_" -ForegroundColor Red
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            Write-Host $reader.ReadToEnd() -ForegroundColor Red
        }
        return $null
    }
}

# 1. CO: Create Cost Center
Write-Host "`n--- CO Module ---" -ForegroundColor Yellow
Test-Endpoint "Post" "/api/controlling/cost-centers" @{ Code = "CC-$suffix"; Name = "IT Department $suffix" }
$cc = Test-Endpoint "Get" "/api/controlling/cost-centers"
Write-Host "Cost Centers: $($cc.data | Out-String)"

# 2. MM: Create Material
Write-Host "`n--- MM Module ---" -ForegroundColor Yellow
$matRes = Test-Endpoint "Post" "/api/materials/materials" @{ MaterialCode = "MAT-$suffix"; Description = "Test Material $suffix"; BaseUnitOfMeasure = "EA"; StandardPrice = 10.0 }
$matId = $matRes.data
Write-Host "Material ID: $matId"

# 3. MM: Post Stock Movement (GR)
if ($matId) {
    Test-Endpoint "Post" "/api/materials/movements" @{ MaterialId = $matId; MovementType = 101; Quantity = 100; MovementDate = [DateTime]::UtcNow.ToString("o") }
    $stock = Test-Endpoint "Get" "/api/materials/stock"
    Write-Host "Stock Levels: $($stock.data | Out-String)"
}

# 4. SD: Create Sales Order
Write-Host "`n--- SD Module ---" -ForegroundColor Yellow
if ($matId) {
    $soRes = Test-Endpoint "Post" "/api/sales/orders" @{ CustomerId = "CUST-$suffix"; Currency = "USD"; Items = @(@{ MaterialCode = "MAT-$suffix"; Quantity = 10 }) }
    $soId = $soRes.data
    Write-Host "Sales Order ID: $soId"

    if ($soId) {
        # 5. SD: Bill Sales Order
        Test-Endpoint "Post" "/api/sales/orders/$soId/bill" @{}
        
        $orders = Test-Endpoint "Get" "/api/sales/orders"
        Write-Host "Sales Orders: $($orders.data | Out-String)"

        # 6. FI: Check Journal Entries
        Write-Host "Waiting for Integration (20s)..." -ForegroundColor Cyan
        Start-Sleep -Seconds 20 
        Write-Host "`n--- FI Module (Integration Check) ---" -ForegroundColor Yellow
        $je = Test-Endpoint "Get" "/api/finance/journal-entries"
        Write-Host "Journal Entries: $($je.data.items | Out-String)"
    }
}
