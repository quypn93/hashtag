# Stop IIS, Publish, and Restart IIS
# Run as Administrator

Write-Host "Stopping IIS App Pool..." -ForegroundColor Yellow
Import-Module WebAdministration
Stop-WebAppPool -Name hashtag
Start-Sleep -Seconds 2

Write-Host "`nPublishing project..." -ForegroundColor Cyan
cd "d:\Task\TrendTag"
dotnet publish "HashTag\HashTag.csproj" -c Release -o "HashTag\bin\Release\net8.0\publish"

# Restore web.config from backup after publish
$publishPath = "HashTag\bin\Release\net8.0\publish"
$webConfigPath = "$publishPath\web.config"
$webConfigBakPath = "$publishPath\web.config.bak"

Write-Host "`nRestoring web.config from backup..." -ForegroundColor Yellow

# Delete the newly generated web.config
if (Test-Path $webConfigPath) {
    Remove-Item $webConfigPath -Force
    Write-Host "  Deleted new web.config" -ForegroundColor Gray
}

# Rename web.config.bak to web.config
if (Test-Path $webConfigBakPath) {
    Rename-Item $webConfigBakPath -NewName "web.config" -Force
    Write-Host "  Renamed web.config.bak to web.config" -ForegroundColor Gray

    # Create new backup from the restored web.config
    Copy-Item $webConfigPath -Destination $webConfigBakPath -Force
    Write-Host "  Created new web.config.bak backup" -ForegroundColor Gray
} else {
    Write-Host "  Warning: web.config.bak not found, keeping new web.config" -ForegroundColor Red
}

Write-Host "`nStarting IIS App Pool..." -ForegroundColor Yellow
Start-WebAppPool -Name hashtag

Write-Host "`nDone! Site updated successfully." -ForegroundColor Green
Write-Host "View site at: http://localhost:8686" -ForegroundColor Cyan
