$port = 9092
$hostName = "127.0.0.1"

Write-Host "Testing TCP connection to $hostName on port $port..."

try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect($hostName, $port)
    if ($tcp.Connected) {
        Write-Host "SUCCESS: Port $port is OPEN and listening." -ForegroundColor Green
        $tcp.Close()
    }
}
catch {
    Write-Host "FAILURE: Could not connect to $hostName on port $port." -ForegroundColor Red
    Write-Host "Error Details: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "`nTroubleshooting Tips:"
    Write-Host "1. Ensure Docker Desktop is running."
    Write-Host "2. Ensure you ran 'docker compose -f Valora/tools/docker-compose.yml up -d'"
    Write-Host "3. Check 'docker ps' to see if container is 'Up'."
}
