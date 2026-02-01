
$mongoUri = "mongodb://localhost:27017"
$dbName = "Valora"
$collectionName = "Entity_Material"
$tenantId = "LAB003"

$json = @"
{
    "TenantId": "$tenantId",
    "MaterialCode": "MAT-SEED-001",
    "Description": "Seeded Material for Lookup Test",
    "MaterialType": "Raw Material",
    "BaseUnitOfMeasure": "KG",
    "StandardPrice": 100.00,
    "CreatedAt": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")",
    "CreatedBy": "script",
    "IsActive": true
}
"@

# Note: We can't easily use Mongo driver here without dependencies.
# So we will use the API to create it, but wait... API creates in SQL and then Syncs.
# Sync is failing/slow.
# Let's try to hit the "CreateMaterial" API again and wait longer.

$baseUrl = "http://localhost:5028"
$headers = @{
    "Content-Type" = "application/json"
    "X-Tenant-Id" = $tenantId
    "X-User-Id" = "user-seed"
}

$body = @{
    MaterialCode = "MAT-LOOKUP-TEST-$(Get-Random)"
    Description = "Lookup Test Material"
    MaterialType = "Finished Goods"
    BaseUnitOfMeasure = "EA"
    StandardPrice = 50.00
    ValuationClass = "3000"
} | ConvertTo-Json

Write-Host "Creating Material..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/materials/materials" -Method Post -Headers $headers -Body $body
    Write-Host "Material Created: $($response.Success)"
    Write-Host "Wait 5s for Kafka..."
    Start-Sleep -Seconds 5
} catch {
    Write-Host "Error: $_"
}
