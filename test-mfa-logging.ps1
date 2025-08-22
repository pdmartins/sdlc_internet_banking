# Test script to demonstrate MFA code logging
# This script will start the API and show how MFA codes are logged

Write-Host "🔐 Testing MFA Code Logging" -ForegroundColor Cyan
Write-Host "=" * 50

Write-Host "`n1. Starting the API server..." -ForegroundColor Yellow
Write-Host "   - MFA codes will be logged with 🔐 (generation) and 📱/📧 (sending) icons"
Write-Host "   - Resent codes will be logged with 🔄 icon"
Write-Host "   - Logs will show the actual 6-digit code for testing purposes"

Write-Host "`n2. Expected log format examples:" -ForegroundColor Green
Write-Host "   warn: 🔐 MFA CODE GENERATED for user@example.com: 123456 (Method: sms) - Session: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
Write-Host "   warn: 📱 SMS MFA CODE for user 12345 (user@example.com): 123456 (would send to phone)"
Write-Host "   warn: 📧 EMAIL MFA CODE for user 12345 (user@example.com): 123456"
Write-Host "   warn: 🔄 MFA CODE RESENT for user@example.com: 654321 (Method: sms) - Session: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

Write-Host "`n3. To test MFA logging:" -ForegroundColor Magenta
Write-Host "   a) Register a new user with MFA enabled"
Write-Host "   b) Login to trigger MFA code generation"
Write-Host "   c) Watch the API logs for the generated codes"
Write-Host "   d) Test the resend functionality to see resent codes"

Write-Host "`n4. Starting API now..." -ForegroundColor Yellow
Write-Host "   Press Ctrl+C to stop the server and return to this script"

# Start the API
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "api/src/ContosoBank.Web/ContosoBank.Web.csproj" -WorkingDirectory "D:\Repos\_av\Avanade\sdlc_internet_banking" -PassThru

Write-Host "`nAPI is starting... Check the logs above for MFA codes!" -ForegroundColor Green
Write-Host "API Process ID: $($apiProcess.Id)" -ForegroundColor Gray

# Wait for user to stop
Write-Host "`nPress any key to stop the API server..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop the API
if (!$apiProcess.HasExited) {
    $apiProcess.Kill()
    Write-Host "`nAPI server stopped." -ForegroundColor Red
}

Write-Host "`n✅ MFA logging test complete!" -ForegroundColor Green
Write-Host "The API now logs all MFA codes prominently for development and testing purposes." -ForegroundColor Cyan
