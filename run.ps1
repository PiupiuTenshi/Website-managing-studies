Write-Host "🚀 Starting Remote Assignment Platform Local Development Environment..." -ForegroundColor Green

# Start Backend
Write-Host "Starting Backend (.NET API) in a new window..." -ForegroundColor Cyan
Start-Process "powershell" -ArgumentList "-NoExit", "-Command", "cd apps/backend-dotnet; dotnet run --project src/WebApi/RemoteAssignment.WebApi.csproj" -WindowStyle Normal

# Start Frontend
Write-Host "Starting Frontend (Next.js) in a new window..." -ForegroundColor Magenta
Start-Process "powershell" -ArgumentList "-NoExit", "-Command", "cd apps/frontend-nextjs; npm run dev" -WindowStyle Normal

Write-Host "✅ Both services have been launched in separate windows!" -ForegroundColor Green
Write-Host "Backend API will be available at: http://localhost:5000 or https://localhost:5001 (check the backend console)"
Write-Host "Frontend App will be available at: http://localhost:3000"
