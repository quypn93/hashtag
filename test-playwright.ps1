# Test Playwright Installation
Write-Host "Testing Playwright on IIS publish folder..." -ForegroundColor Cyan

$publishPath = "D:\Task\TrendTag\HashTag\bin\Release\net8.0\publish"

# Check if playwright.ps1 exists
$playwrightScript = Join-Path $publishPath "playwright.ps1"
if (Test-Path $playwrightScript) {
    Write-Host "✓ playwright.ps1 found at: $playwrightScript" -ForegroundColor Green

    # Try to install browsers
    Write-Host "Installing Chromium..." -ForegroundColor Yellow
    cd $publishPath
    powershell -File playwright.ps1 install chromium

    Write-Host "✓ Chromium installation completed" -ForegroundColor Green
} else {
    Write-Host "✗ playwright.ps1 NOT found" -ForegroundColor Red
}

# Check Playwright cache
$playwrightCache = "$env:USERPROFILE\.cache\ms-playwright"
if (Test-Path $playwrightCache) {
    Write-Host "✓ Playwright cache found at: $playwrightCache" -ForegroundColor Green
    dir $playwrightCache
} else {
    Write-Host "⚠ Playwright cache NOT found" -ForegroundColor Yellow
}

# Check IIS App Pool permissions
Write-Host "`nChecking IIS App Pool permissions..." -ForegroundColor Yellow
$appPoolName = "hashtag"

try {
    Import-Module WebAdministration -ErrorAction Stop
    $appPool = Get-Item "IIS:\AppPools\$appPoolName" -ErrorAction Stop
    Write-Host "✓ App Pool '$appPoolName' exists" -ForegroundColor Green
    Write-Host "  Identity: $($appPool.processModel.identityType)" -ForegroundColor White
} catch {
    Write-Host "⚠ Could not check App Pool" -ForegroundColor Yellow
}

Write-Host "`nNext: Restart IIS App Pool and test crawler" -ForegroundColor Cyan
