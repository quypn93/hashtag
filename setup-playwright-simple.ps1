# Simple Playwright Setup for IIS
# Run as Administrator

Write-Host "Installing Playwright for IIS..." -ForegroundColor Green

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Run as Administrator!" -ForegroundColor Red
    exit 1
}

# Install Playwright CLI
Write-Host "Step 1: Installing Playwright CLI..."
dotnet tool install --global Microsoft.Playwright.CLI 2>&1 | Out-Null
Write-Host "Done"

# Add to PATH
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Install Chromium
Write-Host "Step 2: Installing Chromium browser..."
playwright install chromium
Write-Host "Done"

# Set permissions
Write-Host "Step 3: Setting permissions for IIS..."
$appPoolName = "hashtag"
$playwrightPath = "$env:USERPROFILE\.cache\ms-playwright"

if (Test-Path $playwrightPath) {
    $acl = Get-Acl $playwrightPath
    $permission = "IIS APPPOOL\$appPoolName", "Read,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $playwrightPath $acl
    Write-Host "Permissions set"
}

# Restart IIS App Pool
Write-Host "Step 4: Restarting IIS App Pool..."
Import-Module WebAdministration
Restart-WebAppPool -Name $appPoolName
Write-Host "Done"

Write-Host ""
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "Browse to http://localhost:5686 and test the crawler"
