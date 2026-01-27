---
description: Start the Frontend and Backend intelligently
---

1. Check if the Backend is already running (Default Port 5028/7258)
   - Run `netstat -ano | findstr :5028` (or configured port)
   - If running:
     - Ask user if they want to restart it.
     - If yes, kill process (taskkill /PID <PID> /F) and run `dotnet run`.
     - If no, skip.
   - If not running:
     - Run `dotnet run` in `backend/Valora.Api`.

2. Check if the Frontend is already running (Default Port 5173)
   - Check ports 5173, 5174, 5175...
   - If running:
     - Verify it is THIS project.
     - Ask user if they want to restart.
     - If yes, kill and `npm run dev`.
     - If no, use existing URL.
   - If not running:
     - Run `npm run dev` in `frontend`.

3. Verify Infrastructure (Docker)
   - Check `docker ps`.
   - Ensure Postgres/Mongo/Kafka are up if needed.
