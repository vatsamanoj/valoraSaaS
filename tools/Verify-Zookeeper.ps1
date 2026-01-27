$port = 2181
$hostName = "127.0.0.1"

Write-Host "Testing TCP connection to Zookeeper at $hostName on port $port..."

try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect($hostName, $port)
    if ($tcp.Connected) {
        Write-Host "SUCCESS: Zookeeper is reachable on port $port." -ForegroundColor Green
        
        # Optional: Send "ruok" command to Zookeeper (4-letter word command)
        $stream = $tcp.GetStream()
        $writer = New-Object System.IO.StreamWriter($stream)
        $reader = New-Object System.IO.StreamReader($stream)
        
        $writer.Write("ruok")
        $writer.Flush()
        
        # Set a small timeout for reading
        $tcp.ReceiveTimeout = 2000 
        
        try {
            $response = $reader.ReadToEnd()
            if ($response -eq "imok") {
                 Write-Host "STATUS: Zookeeper responded 'imok'. It is healthy." -ForegroundColor Green
            } else {
                 Write-Host "STATUS: Connected, but received unexpected response: '$response'" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "STATUS: Connected, but read timed out (commands might be disabled)." -ForegroundColor Yellow
        }

        $tcp.Close()
    }
}
catch {
    Write-Host "FAILURE: Could not connect to Zookeeper on port $port." -ForegroundColor Red
    Write-Host "Error Details: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "`nTroubleshooting:"
    Write-Host "1. Ensure 'docker compose' is running."
    Write-Host "2. Zookeeper must be running before Kafka starts."
}
