# Setup Playwright for IIS Application Pool
# Run this as Administrator

Write-Host "=== TrendTag IIS Playwright Setup ===" -ForegroundColor Cyan
Write-Host "This script will install Playwright for IIS Application Pool" -ForegroundColor Yellow
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Step 1: Install Playwright CLI globally
Write-Host "Step 1: Installing Playwright CLI tool globally..." -ForegroundColor Green
try {
    dotnet tool install --global Microsoft.Playwright.CLI --version 1.48.0 2>&1 | Out-Null
    Write-Host "✓ Playwright CLI installed" -ForegroundColor Green
} catch {
    Write-Host "ℹ Playwright CLI already installed or installation failed" -ForegroundColor Yellow
}

# Step 2: Install Chromium to System (for all users)
Write-Host ""
Write-Host "Step 2: Installing Chromium browser for system..." -ForegroundColor Green

# Add dotnet tools to PATH for this session
$env:PATH += ";$env:ProgramFiles\dotnet\tools"
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

try {
    # Install browsers
    & playwright install chromium 2>&1 | Out-Host

    # Also install system dependencies (Windows usually doesn't need this, but just in case)
    Write-Host "✓ Chromium installed" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to install Chromium" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
}

# Step 3: Set permissions for IIS App Pool
Write-Host ""
Write-Host "Step 3: Setting permissions for IIS Application Pool..." -ForegroundColor Green

$appPoolName = "hashtag"  # Your IIS App Pool name
$playwrightPath = "$env:USERPROFILE\.cache\ms-playwright"
$systemPlaywrightPath = "$env:LOCALAPPDATA\..\Local\ms-playwright"

# Grant permissions to IIS App Pool identity
$paths = @($playwrightPath, $systemPlaywrightPath, "$env:TEMP")

foreach ($path in $paths) {
    if (Test-Path $path) {
        try {
            $acl = Get-Acl $path
            $permission = "IIS APPPOOL\$appPoolName", "Read,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
            $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
            $acl.SetAccessRule($accessRule)
            Set-Acl $path $acl
            Write-Host "✓ Granted permissions to $path" -ForegroundColor Green
        } catch {
            Write-Host "ℹ Could not set permissions for $path (may not exist yet)" -ForegroundColor Yellow
        }
    }
}

# Step 4: Update appsettings for crawler
Write-Host ""
Write-Host "Step 4: Checking appsettings.json..." -ForegroundColor Green

$publishPath = "D:\Task\TrendTag\HashTag\bin\Release\net8.0\publish"
$appsettingsPath = Join-Path $publishPath "appsettings.json"

if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

    if ($appsettings.CrawlerSettings.EnabledSources.Count -eq 0) {
        Write-Host "⚠ Crawler is DISABLED in appsettings.json" -ForegroundColor Yellow
        Write-Host "  EnabledSources: []" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To enable crawler, update appsettings.json:" -ForegroundColor Yellow
        Write-Host '  "EnabledSources": ["TikTok"]' -ForegroundColor White
    } else {
        Write-Host "✓ Crawler is enabled" -ForegroundColor Green
    }
} else {
    Write-Host "⚠ appsettings.json not found at $appsettingsPath" -ForegroundColor Yellow
}

# Step 5: Restart IIS App Pool
Write-Host ""
Write-Host "Step 5: Restarting IIS Application Pool..." -ForegroundColor Green

try {
    Import-Module WebAdministration -ErrorAction Stop

    if (Test-Path "IIS:\AppPools\$appPoolName") {
        Restart-WebAppPool -Name $appPoolName
        Write-Host "✓ Application Pool '$appPoolName' restarted" -ForegroundColor Green
    } else {
        Write-Host "⚠ Application Pool '$appPoolName' not found" -ForegroundColor Yellow
        Write-Host "Available App Pools:" -ForegroundColor Yellow
        Get-ChildItem IIS:\AppPools | Select-Object Name | Format-Table
    }
} catch {
    Write-Host "ℹ Could not restart app pool automatically" -ForegroundColor Yellow
    Write-Host "Please restart it manually in IIS Manager" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Browse to your IIS site: http://localhost:5686" -ForegroundColor White
Write-Host "2. Navigate to /Admin/Login" -ForegroundColor White
Write-Host "3. Login and go to /Admin/Crawler" -ForegroundColor White
Write-Host "4. Click 'Run Crawler Now' to test" -ForegroundColor White
Write-Host ""
Write-Host "If crawler still fails, check:" -ForegroundColor Yellow
Write-Host "- IIS App Pool identity has permissions to Playwright cache" -ForegroundColor White
Write-Host "- Chromium is installed: playwright install chromium" -ForegroundColor White
Write-Host "- Check IIS logs for detailed error messages" -ForegroundColor White
