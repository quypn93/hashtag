# Install Playwright for Production
# Run this script on your hosting server to install Playwright browsers

Write-Host "Installing Playwright CLI tool..." -ForegroundColor Green
dotnet tool install --global Microsoft.Playwright.CLI --version 1.48.0

Write-Host "`nInstalling Chromium browser..." -ForegroundColor Green
playwright install chromium

Write-Host "`nInstalling Chromium dependencies (if Linux)..." -ForegroundColor Green
playwright install-deps chromium

Write-Host "`nPlaywright installation completed!" -ForegroundColor Green
Write-Host "You can now run the crawler." -ForegroundColor Green
