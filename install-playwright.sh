#!/bin/bash
# Install Playwright for Production (Linux)
# Run this script on your hosting server to install Playwright browsers

echo "Installing Playwright CLI tool..."
dotnet tool install --global Microsoft.Playwright.CLI --version 1.48.0

# Add dotnet tools to PATH
export PATH="$PATH:$HOME/.dotnet/tools"

echo "Installing Chromium browser..."
playwright install chromium

echo "Installing Chromium system dependencies..."
playwright install-deps chromium

echo "Playwright installation completed!"
echo "You can now run the crawler."
