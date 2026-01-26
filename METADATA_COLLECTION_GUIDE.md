# Metadata Collection Status & Improvement Guide

## Current Status

### ✅ Sources with ViewCount/PostCount Metadata
| Source | ViewCount | PostCount | Category | Notes |
|--------|-----------|-----------|----------|-------|
| **TikTok Creative Center** | ✅ Yes | ✅ Yes | ✅ Yes | Full metadata available |

### ❌ Sources WITHOUT Metadata (Tag + Rank only)
| Source | ViewCount | PostCount | Potential to Add |
|--------|-----------|-----------|------------------|
| Google Trends | ❌ No | ❌ No | 🟡 Maybe (search volume available) |
| TokChart | ❌ No | ❌ No | 🟢 Yes (view counts visible on page) |
| Countik | ❌ No | ❌ No | 🟢 Yes (stats available) |
| Buffer | ❌ No | ❌ No | 🔴 No (curated list only) |
| Trollishly | ❌ No | ❌ No | 🔴 No (blog post) |
| CapCut | ❌ No | ❌ No | 🟡 Maybe (need investigation) |

---

## Impact on Metrics Calculation

### Current Behavior
- **Metrics service** only calculates for hashtags with `ViewCount.HasValue`
- **Repository query** `GetHashtagsWithRecentDataAsync()` filters: `WHERE ViewCount IS NOT NULL`
- **Result**: Only TikTok hashtags get difficulty/trending metrics

### Metrics That Require ViewCount
1. **Difficulty Level** (Easy/Medium/Hard/Very Hard) - requires ViewCount
2. **Difficulty Score** (1-100) - requires ViewCount
3. **Engagement Rate** - requires ViewCount ÷ PostCount
4. **Predicted View Range** - requires ViewCount + PostCount
5. **Growth Rate** - requires historical ViewCount data

### Metrics That DON'T Require ViewCount
- **Trending Direction** - can use Rank instead (but less accurate)
- **Rank Trend** - based on rank position changes

---

## Improvement Plan

### Phase 1: Add Metadata to High-Value Sources 🎯

#### 1.1 TokChart Enhancement
**Current**: Extracts hashtag tags only
**Available**: View counts visible on the page

**Implementation**:
```javascript
// In CrawlTokChartHashtags, enhance selector to extract view counts
const hashtagData = await page.$$eval('.hashtag-card', cards =>
    cards.map(card => ({
        tag: card.querySelector('.tag')?.textContent,
        viewCount: parseInt(card.querySelector('.view-count')?.textContent.replace(/[^0-9]/g, '')),
        postCount: parseInt(card.querySelector('.post-count')?.textContent.replace(/[^0-9]/g, ''))
    }))
);
```

**Steps**:
1. Open https://tokchart.com/dashboard/hashtags/most-views in browser
2. Inspect page to find view count selectors
3. Update `CrawlTokChartHashtags()` to extract ViewCount/PostCount
4. Test and verify data accuracy

---

#### 1.2 Countik Enhancement
**Current**: Extracts hashtag tags only
**Available**: Stats visible on hashtag pages

**Implementation**:
```javascript
// Similar approach - find stat elements
const stats = await page.$$eval('.stat-item', items =>
    items.map(item => ({
        label: item.querySelector('.label')?.textContent,
        value: parseInt(item.querySelector('.value')?.textContent.replace(/[^0-9]/g, ''))
    }))
);
```

---

#### 1.3 Google Trends Enhancement (Optional)
**Current**: Extracts trending searches
**Available**: Search volume/interest over time

**Challenge**: Google Trends shows "Interest" (0-100 scale), not absolute views
**Solution**: Use Interest as a proxy metric

```csharp
// Map Interest score to estimated view range
ViewCount = MapInterestToViewCount(interestScore) // e.g., 100 = 1M+, 50 = 500K+
```

---

### Phase 2: Fallback Metrics for Non-Metadata Sources

For sources that can't provide ViewCount, create alternative metrics:

#### Rank-Based Difficulty
```csharp
public string CalculateDifficultyFromRank(int rank, string sourceName)
{
    // Source-specific thresholds
    return (sourceName, rank) switch
    {
        ("GoogleTrends", <= 5) => "Very Hard",
        ("GoogleTrends", <= 20) => "Hard",
        ("GoogleTrends", <= 50) => "Medium",
        _ => "Easy"
    };
}
```

#### Trending Score from Rank Changes
```csharp
public async Task<string> CalculateTrendingFromRankAsync(int hashtagId)
{
    // Track rank position changes over time
    var history = await GetHashtagRankHistoryAsync(hashtagId, 7);
    var avgOldRank = history.Take(history.Count / 2).Average(h => h.Rank);
    var avgNewRank = history.Skip(history.Count / 2).Average(h => h.Rank);

    // Lower rank = better (rank 1 is top)
    return avgNewRank < avgOldRank ? "Rising" :
           avgNewRank > avgOldRank ? "Falling" : "Stable";
}
```

---

### Phase 3: Mixed Metrics Strategy

Create composite metrics that work with or without ViewCount:

```csharp
public class HashtagMetrics
{
    // Primary metrics (require ViewCount)
    public long? ViewCount { get; set; }
    public int? DifficultyScore { get; set; } // 1-100, requires ViewCount

    // Fallback metrics (use Rank)
    public int? RankScore { get; set; } // 1-100, based on rank across sources
    public string TrendingDirection { get; set; } // Can use either ViewCount or Rank

    // Combined metric
    public int OverallScore => DifficultyScore ?? RankScore ?? 50;
}
```

---

## Quick Win: Immediate Improvements

### 1. Update TikTok Crawler to Get More Hashtags
**Current**: Scrolls 20 times
**Improvement**: Scroll until no new hashtags appear

```csharp
int previousCount = 0;
int noChangeCount = 0;
const int maxNoChange = 3;

while (noChangeCount < maxNoChange)
{
    var currentCount = await page.EvaluateAsync<int>("() => document.querySelectorAll('.hashtag-item').length");

    if (currentCount == previousCount)
    {
        noChangeCount++;
    }
    else
    {
        noChangeCount = 0;
        previousCount = currentCount;
    }

    await ScrollAndWait(page);
}
```

### 2. Add TikTok Detail Page Crawling
**Purpose**: Get even more accurate metadata

```csharp
private async Task<HashtagDetails> CrawlTikTokHashtagDetail(string hashtag)
{
    await page.GotoAsync($"https://ads.tiktok.com/business/creativecenter/hashtag/{hashtag}");

    return new HashtagDetails
    {
        ViewCount = await GetViewCount(page),
        PostCount = await GetPostCount(page),
        TopCreators = await GetTopCreators(page),
        RelatedHashtags = await GetRelatedHashtags(page)
    };
}
```

---

## Testing Checklist

### After Adding Metadata to a Source

- [ ] ViewCount is correctly parsed (handle K/M/B suffixes)
- [ ] PostCount is correctly parsed
- [ ] NULL values handled gracefully (missing data)
- [ ] Metrics calculation works with new data
- [ ] Database constraints not violated (check max values)
- [ ] Logs show successful data collection
- [ ] Metrics dashboard displays new data

### Sample Test Cases

```csharp
[Fact]
public void ParseViewCount_ShouldHandleKMB()
{
    Assert.Equal(1500, ParseViewCount("1.5K"));
    Assert.Equal(2000000, ParseViewCount("2M"));
    Assert.Equal(1500000000, ParseViewCount("1.5B"));
}

[Fact]
public async Task CalculateMetrics_ShouldSkipNullViewCount()
{
    var hashtag = new Hashtag { Id = 1 };
    var history = new HashtagHistory { ViewCount = null };

    var result = await _metricsService.CalculateMetricsForHashtagAsync(1);

    Assert.False(result); // Should return false for NULL ViewCount
}
```

---

## Recommended Priority

1. **🔥 High Priority**: TokChart metadata (most likely to have view stats)
2. **🟡 Medium Priority**: Countik metadata
3. **🔵 Low Priority**: Google Trends interest score mapping
4. **⚪ Nice to Have**: TikTok detail page crawling for richer data

---

## Current Workaround

Until other sources add metadata:

### Admin Dashboard Notice
Add a notification on the metrics page:

> ℹ️ **Note**: Difficulty metrics are currently only available for TikTok hashtags.
> Other sources (Google Trends, TokChart, etc.) provide hashtag trends but not view/post statistics.
> We're working to add metadata from more sources.

### Filter UI
Add a badge/filter to show which hashtags have metrics:

```html
<span class="badge bg-success">Has Metrics</span>  <!-- TikTok -->
<span class="badge bg-secondary">Rank Only</span>  <!-- Others -->
```

---

## Summary

- ✅ **Current**: Only TikTok has full metadata (ViewCount, PostCount, Category)
- 🎯 **Goal**: Add metadata to TokChart and Countik (high value, feasible)
- 🔄 **Alternative**: Use rank-based metrics for sources without ViewCount
- 📊 **Impact**: More hashtags will have difficulty/trending metrics
- 🕐 **Estimate**: 2-4 hours per source to add metadata collection

**Next Action**: Choose Phase 1.1 (TokChart) or Phase 2 (Rank-based fallback) based on your needs.
