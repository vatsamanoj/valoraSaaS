$hostName = "db.ndzbkywblnbbshpxuqzk.supabase.co"
$port = 5432

Write-Host "Testing TCP connection to $hostName on port $port..."

try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $connectTask = $tcp.ConnectAsync($hostName, $port)
    $wait = $connectTask.Wait(5000) # 5 second timeout
    
    if ($wait -and $tcp.Connected) {
        Write-Host "SUCCESS: Supabase is reachable on port $port." -ForegroundColor Green
        $tcp.Close()
    } else {
        Write-Host "FAILURE: Connection timed out after 5 seconds." -ForegroundColor Red
    }
}
catch {
    Write-Host "FAILURE: Could not connect to Supabase." -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
}
