# Crawler Configuration Guide

## TikTok Crawler - Cookie Authentication

The TikTok Creative Center crawler supports authenticated sessions using cookies. When logged in, you can access more hashtags through infinite scroll (no "View More" button limitation).

### How to Get TikTok Cookies

1. **Log in to TikTok Creative Center**:
   - Go to https://ads.tiktok.com/business/creativecenter/
   - Log in with your account

2. **Export Cookies** (Chrome):
   - Press F12 to open DevTools
   - Go to Application tab → Cookies → https://ads.tiktok.com
   - Copy all cookies

3. **Convert to JSON Format**:
   ```json
   [
     {
       "name": "cookie_name",
       "value": "cookie_value",
       "domain": ".tiktok.com",
       "path": "/",
       "expires": 1234567890,
       "httpOnly": false,
       "secure": true,
       "sameSite": "Lax"
     }
   ]
   ```

4. **Add to Configuration**:
   - Open `HashTag/appsettings.json`
   - Paste the cookie JSON array into `CrawlerSettings.TikTokCookies`
   - **Important**: Escape double quotes by using `\"` or use single quotes in JSON

### Example Configuration

```json
{
  "CrawlerSettings": {
    "TikTokCookies": "[{\"name\":\"sessionid\",\"value\":\"your_session_value\",\"domain\":\".tiktok.com\",\"path\":\"/\",\"secure\":true}]"
  }
}
```

### Alternative: Use Cookie Export Extension

1. Install "EditThisCookie" or "Cookie-Editor" Chrome extension
2. Export cookies as JSON
3. Copy the array and minify it (remove newlines)
4. Paste into appsettings.json

---

## TokChart Crawler - Multiple URLs

The TokChart crawler now fetches from both:
- Most viewed hashtags: `https://tokchart.com/dashboard/hashtags/most-views`
- Growing hashtags: `https://tokchart.com/dashboard/hashtags/growing`

No configuration needed - it automatically crawls both URLs.

---

## Google Trends Crawler - Pagination

The Google Trends crawler now supports pagination to fetch more trending searches:
- Automatically clicks "Next page" button 3 times
- Collects ~75 trending searches instead of ~25
- Uses selectors: `button[jsname='ViaHrd'][aria-label='Go to next page']`

No configuration needed.

---

## Crawler Schedule

Configure in `appsettings.json`:

```json
{
  "CrawlerSettings": {
    "ScheduleIntervalHours": 6,
    "RunOnStartup": false,
    "EnabledSources": ["TikTok", "GoogleTrends", "Buffer", "Trollishly", "CapCut", "TokChart", "Countik"]
  }
}
```

- **ScheduleIntervalHours**: How often to run (default: 6 hours)
- **RunOnStartup**: Run crawler immediately when app starts
- **EnabledSources**: Which sources to crawl
