# TrendTag Production Deployment Guide

## 1. Prerequisites

- .NET 8.0 Runtime
- SQL Server database
- Playwright browsers (Chromium)

## 2. Playwright Installation

Playwright is required for the TikTok crawler to work.

### Windows Server

Run PowerShell as Administrator:

```powershell
cd path\to\TrendTag
.\install-playwright.ps1
```

### Linux Server

```bash
cd /path/to/TrendTag
chmod +x install-playwright.sh
./install-playwright.sh
```

### Manual Installation

If the scripts fail, run these commands manually:

```bash
# Install Playwright CLI
dotnet tool install --global Microsoft.Playwright.CLI

# Add to PATH (Linux/Mac)
export PATH="$PATH:$HOME/.dotnet/tools"

# Install Chromium browser
playwright install chromium

# Install system dependencies (Linux only)
playwright install-deps chromium
```

### Verify Installation

```bash
playwright --version
```

Should output: `Version 1.48.0`

## 3. Database Configuration

Update `appsettings.json` with production values:

```json
{
  "SiteUrl": "https://yourdomain.com",
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_PRODUCTION_CONNECTION_STRING"
  },
  "AdminSettings": {
    "Password": "YOUR_SECURE_PASSWORD"
  },
  "OpenAI": {
    "ApiKey": "YOUR_GROQ_API_KEY"
  }
}
```

## 4. Run Application

```bash
dotnet HashTag.dll
```

## 5. Crawler Schedule

The crawler runs automatically every 6 hours as configured in `CrawlerSettings.ScheduleIntervalHours`.

To run crawler manually, go to: `/Admin/Crawler`

## 6. Common Issues

### Issue: "Executable doesn't exist at chromium_headless_shell"

**Solution**: Playwright not installed. Run the installation script above.

### Issue: Query cost exceeds 300

**Solution**: Already fixed in latest version with query optimizations.

### Issue: Crawler fails with timeout

**Solution**: Increase timeout in `appsettings.json`:

```json
"CrawlerSettings": {
  "TimeoutSeconds": 180
}
```

## 7. Performance Tips

- Enable Response Compression (already configured)
- Use SQL Server indexes (already configured)
- Enable caching for sitemap (already configured)
- Limit query results with Take() (already optimized)

## 8. Security Checklist

- ✅ Change default admin password
- ✅ Use environment-specific connection strings
- ✅ Keep API keys secure
- ✅ Enable HTTPS
- ✅ Set `AllowedHosts` in production
- ✅ Disable debug logging in production

## 9. Monitoring

Check logs for errors:
- Application logs in your hosting provider dashboard
- Database query performance
- Crawler success/failure rates at `/Admin/Crawler`

## 10. Support

For issues, check:
- Application logs
- Database connection
- Playwright installation
- TikTok cookies validity (expires after ~30 days)
