
$baseUrl = "http://localhost:5028"
$tenantId = "LAB_001"
$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $tenantId
}

Write-Host "Checking SalesOrder Schema..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/debug/schema/SalesOrder" -Method Get -Headers $headers
    Write-Host "Schema Found. ShouldPost: $($response.shouldPost)"
    # Write-Host $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error checking schema: $_"
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            Write-Host "Response Body: $body"
        }
    } catch {}
}
