# TikTok Crawler Fix Proposal

## Problem
Crawler chỉ thu thập được 93 hashtags (từ trending page) thay vì 1500+ hashtags (trending + 18 industries) vì:
- UI của TikTok Creative Center đã thay đổi
- Không tìm được Industry filter dropdown
- Không click được vào industry options

## Root Cause
Code hiện tại cố gắng:
1. Navigate đến base URL
2. Tìm và click Industry dropdown (`div.byted-select-popover-panel-inner`, etc.)
3. Click vào industry option cụ thể
4. Chờ API response có `industry_id` parameter

**Vấn đề**: Selectors không còn đúng sau khi TikTok update UI.

## Solution Options

### Option 1: Update Selectors (Temporary Fix)
- Inspect TikTok UI mới và tìm selectors mới
- Update code với selectors mới
- **Nhược điểm**: Sẽ lại bị break khi TikTok update UI lần nữa

### Option 2: Direct API Call (Recommended)
Thay vì click UI, gọi API trực tiếp:

```csharp
// Current approach (UI-based):
// 1. Navigate to page
// 2. Click Industry filter
// 3. Click industry option
// 4. Wait for API response

// New approach (API-based):
// 1. Call API directly with industry_id parameter
```

**API Endpoint**:
```
GET https://ads.tiktok.com/creative_radar_api/v1/popular_trend/hashtag/list
Parameters:
  - country_code: VN
  - period: 7 (last 7 days)
  - industry_id: {industry_id} (e.g., "10000000000" for Education)
  - page: 1
  - limit: 50
```

**Benefits**:
- Không phụ thuộc vào UI changes
- Nhanh hơn (không cần Playwright browser)
- Dễ maintain
- Có thể pagination để lấy tất cả hashtags

### Option 3: Hybrid Approach
- Giữ Playwright để load cookies và authenticate
- Sau đó dùng HttpClient để call API trực tiếp
- **Best of both worlds**: Authentication + Stable API calls

## Recommended Implementation

### Step 1: Refactor CrawlTikTokIndustryHashtags
```csharp
private async Task<List<HashtagRaw>> CrawlTikTokIndustryHashtags(
    IPage page,
    string industryId,
    string industryName)
{
    var result = new List<HashtagRaw>();

    // Navigate once to set cookies
    var baseUrl = "https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?country_code=VN&period=7";
    await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    await Task.Delay(2000);

    // Call API directly instead of clicking UI
    var apiUrl = $"https://ads.tiktok.com/creative_radar_api/v1/popular_trend/hashtag/list?country_code=VN&period=7&industry_id={industryId}&page=1&limit=100";

    var response = await page.EvaluateAsync<string>(@"
        async (url) => {
            const res = await fetch(url);
            return await res.text();
        }
    ", apiUrl);

    // Parse JSON response
    // Extract hashtags
    // Return results
}
```

### Step 2: Add Pagination Support
```csharp
// Loop through pages until no more results
int page = 1;
while (true)
{
    var apiUrl = $"...&page={page}&limit=100";
    var response = await CallAPI(apiUrl);

    if (response.hashtags.Count == 0)
        break;

    result.AddRange(response.hashtags);
    page++;
}
```

## Quick Fix (Minimal Changes)

If you want quickest fix without major refactoring:

1. Update selectors để match UI mới của TikTok
2. Thêm nhiều fallback selectors hơn
3. Thêm logging để debug khi selectors fail

```csharp
var industryFilterSelectors = new[]
{
    // Add more selectors based on current TikTok UI
    "button:has-text('Industry')",
    "[aria-label='Industry']",
    "[data-filter='industry']",
    "div:has-text('Industry') button",
    // ... add more variants
};
```

## Next Steps

**Recommended**: Implement Option 2 (Direct API Call)
- More stable
- Faster
- Less maintenance

**Alternative**: Quick fix Option 1
- Inspect current TikTok UI
- Update selectors
- Test thoroughly

Bạn muốn tôi implement solution nào?
