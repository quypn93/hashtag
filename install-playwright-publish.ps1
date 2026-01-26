# Install Playwright for IIS Site (in publish folder)
# Run as Administrator

Write-Host "Installing Playwright in publish folder..." -ForegroundColor Cyan

$publishPath = "D:\Task\TrendTag\HashTag\bin\Release\net8.0\publish"
$playwrightPath = "$publishPath\playwright"

# Create folder if not exists
if (-not (Test-Path $playwrightPath)) {
    New-Item -ItemType Directory -Force -Path $playwrightPath
    Write-Host "Created folder: $playwrightPath" -ForegroundColor Green
}

# Set environment variable (use relative path)
$env:PLAYWRIGHT_BROWSERS_PATH = "playwright"

# Install Chromium
Write-Host "Installing Chromium to: $playwrightPath" -ForegroundColor Yellow
cd $publishPath
powershell -File playwright.ps1 install chromium

# Verify Chromium was installed
if (-not (Test-Path "$playwrightPath\chromium*")) {
    Write-Host "WARNING: Chromium folder not found, trying with full path..." -ForegroundColor Yellow
    $env:PLAYWRIGHT_BROWSERS_PATH = $playwrightPath
    powershell -File playwright.ps1 install chromium
}

# Grant permissions for IIS user
Write-Host "Setting permissions..." -ForegroundColor Yellow
icacls $playwrightPath /grant "trendhashtag:(OI)(CI)RX" /T
icacls $playwrightPath /grant "IIS APPPOOL\hashtag:(OI)(CI)RX" /T

# Restart IIS App Pool
Write-Host "Restarting IIS App Pool..." -ForegroundColor Yellow
Import-Module WebAdministration
Restart-WebAppPool -Name hashtag

# Verify
Write-Host "`nVerifying installation..." -ForegroundColor Green
if (Test-Path "$playwrightPath\chromium*") {
    Write-Host "SUCCESS! Chromium installed at: $playwrightPath" -ForegroundColor Green
    dir $playwrightPath
} else {
    Write-Host "WARNING: Chromium not found in $playwrightPath" -ForegroundColor Red
}

Write-Host "`nDone! Test crawler at: http://localhost:8686/Admin/Crawler" -ForegroundColor Cyan
