# Contoso Bank - Internet Banking Application Setup

Write-Host "🏦 Setting up Contoso Bank Internet Banking Application..." -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js detected: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js not found. Please install Node.js 18+ from https://nodejs.org" -ForegroundColor Red
    exit 1
}

# Navigate to client directory
Set-Location "client"

Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Dependencies installed successfully!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🚀 Starting the development server..." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "The application will open at: http://localhost:3000" -ForegroundColor Blue
    Write-Host "Welcome screen (User Story 1.1.1) is now ready!" -ForegroundColor Green
    Write-Host ""
    
    # Start the development server
    npm start
} else {
    Write-Host "❌ Failed to install dependencies. Please check your internet connection and try again." -ForegroundColor Red
    exit 1
}
