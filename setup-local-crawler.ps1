# Setup Playwright for Local Crawler
# Run this once to install Playwright on your local machine

Write-Host "=== TrendTag Local Crawler Setup ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Install Playwright CLI
Write-Host "Step 1: Installing Playwright CLI tool..." -ForegroundColor Green
try {
    dotnet tool install --global Microsoft.Playwright.CLI 2>&1 | Out-Null
    Write-Host "✓ Playwright CLI installed successfully" -ForegroundColor Green
} catch {
    Write-Host "ℹ Playwright CLI already installed or installation failed" -ForegroundColor Yellow
}

# Step 2: Add to PATH
Write-Host ""
Write-Host "Step 2: Adding dotnet tools to PATH..." -ForegroundColor Green
$dotnetToolsPath = "$env:USERPROFILE\.dotnet\tools"
if ($env:PATH -notlike "*$dotnetToolsPath*") {
    $env:PATH += ";$dotnetToolsPath"
    Write-Host "✓ Added to PATH for this session" -ForegroundColor Green
    Write-Host "ℹ Note: You may need to restart your terminal for permanent effect" -ForegroundColor Yellow
} else {
    Write-Host "✓ Already in PATH" -ForegroundColor Green
}

# Step 3: Install Chromium
Write-Host ""
Write-Host "Step 3: Installing Chromium browser..." -ForegroundColor Green
try {
    & playwright install chromium 2>&1 | Out-Host
    Write-Host "✓ Chromium installed successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to install Chromium. Make sure playwright CLI is in PATH" -ForegroundColor Red
    Write-Host "Try running: playwright install chromium" -ForegroundColor Yellow
}

# Step 4: Verify installation
Write-Host ""
Write-Host "Step 4: Verifying installation..." -ForegroundColor Green
try {
    $version = & playwright --version 2>&1
    Write-Host "✓ Playwright version: $version" -ForegroundColor Green
} catch {
    Write-Host "✗ Playwright not found in PATH" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update appsettings.json with your configuration" -ForegroundColor White
Write-Host "2. Run the application: dotnet run --project HashTag" -ForegroundColor White
Write-Host "3. Navigate to https://localhost:7125/Admin/Crawler" -ForegroundColor White
Write-Host "4. Click 'Run Crawler Now' to start crawling" -ForegroundColor White
Write-Host ""
Write-Host "The crawler will update the production database automatically!" -ForegroundColor Green
