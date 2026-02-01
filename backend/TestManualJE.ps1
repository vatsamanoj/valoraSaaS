function Test-Endpoint {
    param (
        [string]$Method,
        [string]$Path,
        [hashtable]$Body = @{}
    )

    $headers = @{
        "Content-Type"  = "application/json"
        "X-Tenant-Id"   = "LAB_001"
        "X-User-Id"     = "test-user"
        "X-Role"        = "TenantAdmin"
    }

    $url = "http://localhost:5028$Path"
    
    try {
        $params = @{
            Uri     = $url
            Method  = $Method
            Headers = $headers
        }

        if ($Method -ne "Get") {
            $json = $Body | ConvertTo-Json -Depth 10
            $params.Body = $json
        }

        $response = Invoke-RestMethod @params
        Write-Host "[$Method] $Path" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "[$Method] $Path Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
             $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
             $err = $reader.ReadToEnd()
             Write-Host "Response: $err" -ForegroundColor Red
        }
        return $null
    }
}

# 1. Create a Manual Journal Entry
Write-Host "Creating Manual Journal Entry..."
$je = @{
    postingDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    documentNumber = "JE-MANUAL-$(Get-Random)"
    description = "Manual Entry Test"
    reference = "REF-001"
    lines = @(
        @{
            glAccountId = "f0081085-5a6b-4c07-a55f-83c60ef691bd" # Use existing Account ID from Mongo check if possible, or fetch first
            debit = 50.00
            credit = 0.00
            description = "Debit AR"
        },
        @{
            glAccountId = "18d95b96-e748-4092-ba48-5e98db007038" # Use existing Account ID
            debit = 0.00
            credit = 50.00
            description = "Credit Revenue"
        }
    )
}

# Fetch GL Accounts to get valid IDs
$gls = Test-Endpoint "Get" "/api/finance/gl-accounts"
# The API returns the array directly in data, not wrapped in items
if ($gls.data -and $gls.data.Count -ge 2) {
    $je.lines[0].glAccountId = $gls.data[0].id
    $je.lines[1].glAccountId = $gls.data[1].id
} else {
    Write-Host "GL Data: $($gls | Out-String)"
    Write-Error "Not enough GL Accounts found to test."
    exit
}

$res = Test-Endpoint "Post" "/api/finance/journal-entries" $je

if ($res.success) {
    Write-Host "JE Created. ID: $($res.data.id)"
    
    Write-Host "Waiting for Projection (10s)..."
    Start-Sleep -Seconds 10

    $list = Test-Endpoint "Get" "/api/finance/journal-entries"
    $found = $list.data.items | Where-Object { $_.documentNumber -like "JE-MANUAL-*" }
    
    if ($found) {
        Write-Host "SUCCESS: Manual JE found in projection." -ForegroundColor Green
        $found | Out-String | Write-Host
    } else {
        Write-Host "FAILURE: Manual JE NOT found in projection." -ForegroundColor Red
    }
}
