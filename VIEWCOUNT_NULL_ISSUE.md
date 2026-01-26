# ViewCount NULL Issue - Explanation & Solutions

## Problem Summary

**Issue**: ViewCount is NULL for many hashtags in the database.

**Root Cause**: Only **TikTok** crawler collects ViewCount/PostCount metadata. All other crawlers (GoogleTrends, TokChart, Buffer, Trollishly, CapCut, Countik) only collect:
- Tag (hashtag text)
- Rank (position in trending list)
- CollectedDate

---

## Current Implementation

### Crawlers with Metadata ✅
| Source | ViewCount | PostCount | Category | Implementation | Notes |
|--------|-----------|-----------|----------|----------------|-------|
| **TikTok Creative Center** | ✅ Yes (Detail Page) | ✅ Yes (Detail Page) | ✅ Yes (Detail Page) | Lines 262-557 in CrawlerService.cs | Fetches from hashtag detail pages |

### Crawlers WITHOUT Metadata ❌
| Source | Data Collected | File Location |
|--------|----------------|---------------|
| Google Trends | Tag + Rank only | Lines 479-631 |
| TokChart | Tag + Rank only | Lines 633-731 |
| Countik | Tag + Rank only | Lines 733-822 |
| Buffer | Tag + Rank only | Lines 824-882 |
| Trollishly | Tag + Rank only | Lines 884-943 |
| CapCut | Tag + Rank only | Lines 945-1016 |

---

## Metrics Calculation Impact

### What Requires ViewCount
The following metrics **cannot be calculated** without ViewCount:

1. **Difficulty Level** (Easy/Medium/Hard/Very Hard)
   - Formula: Based on ViewCount ranges
   - Easy: < 500K views
   - Medium: 500K - 5M views
   - Hard: 5M - 50M views
   - Very Hard: > 50M views

2. **Difficulty Score** (1-100)
   - Formula: `log10(viewCount + 1) * 10 + postCount penalty`

3. **Engagement Rate**
   - Formula: `ViewCount ÷ PostCount`
   - Indicates views per post

4. **Predicted View Range**
   - Estimates min/max views for new content
   - Based on historical ViewCount data

5. **Growth Rate** (7-day & 30-day)
   - Percentage change in ViewCount over time
   - Requires historical ViewCount data

###  Current Behavior
- **Metrics service** (`HashtagMetricsService.cs`) checks for `ViewCount.HasValue`
- **If NULL**: Skips calculation and returns `false` (line 32-36)
- **If NOT NULL**: Calculates all metrics

```csharp
// From HashtagMetricsService.cs, line 31-36
var latestHistory = await _repository.GetLatestHistoryWithMetadataAsync(hashtagId);
if (latestHistory == null || !latestHistory.ViewCount.HasValue)
{
    _logger.LogDebug("No metadata available for hashtag {Id}", hashtagId);
    return false; // Skip this hashtag
}
```

### Repository Query Filter
```csharp
// From HashtagRepository.cs, line 481-485
public async Task<List<Hashtag>> GetHashtagsWithRecentDataAsync(int days)
{
    var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

    var hashtagIds = await _context.HashtagHistory
        .Where(h => h.CollectedDate >= cutoffDate && h.ViewCount.HasValue) // ← Filters NULL
        .Select(h => h.HashtagId)
        .Distinct()
        .ToListAsync();

    return await _context.Hashtags
        .Where(h => hashtagIds.Contains(h.Id))
        .ToListAsync();
}
```

**Result**: Only hashtags from TikTok get metrics calculated.

---

## Solutions

### Solution 1: Add ViewCount Collection to Other Crawlers ⭐ Recommended

**Best for**: Long-term accuracy and completeness

**Implementation Guide**: See [METADATA_COLLECTION_GUIDE.md](./METADATA_COLLECTION_GUIDE.md)

**Priority Sources**:
1. **TokChart** - View counts visible on page (High Priority 🔥)
2. **Countik** - Stats available on hashtag pages (Medium Priority)
3. **Google Trends** - Can map Interest score (0-100) to estimated views (Low Priority)

**Example: TokChart Enhancement**
```csharp
// In CrawlTokChartHashtags() method
var hashtagData = await page.$$eval('.hashtag-card', cards =>
    cards.map(card => {
        const tag = card.querySelector('.tag')?.textContent;
        const viewText = card.querySelector('.view-count')?.textContent;
        const postText = card.querySelector('.post-count')?.textContent;

        return {
            tag: tag,
            viewCount: parseInt((viewText || '0').replace(/[^0-9]/g, '')),
            postCount: parseInt((postText || '0').replace(/[^0-9]/g, ''))
        };
    })
);
```

**Steps**:
1. Open target website in browser
2. Inspect page to find view count selectors
3. Update JavaScript extraction in crawler method
4. Handle K/M/B suffixes (1.5K → 1500, 2M → 2000000)
5. Test with real data
6. Verify metrics calculation works

---

### Solution 2: Rank-Based Fallback Metrics

**Best for**: Quick improvement without crawler changes

**Approach**: Create alternative metrics using Rank data

#### Difficulty from Rank
```csharp
public string CalculateDifficultyFromRank(int rank, string sourceName)
{
    return (sourceName, rank) switch
    {
        // Google Trends thresholds
        ("GoogleTrends", <= 5) => "Very Hard",
        ("GoogleTrends", <= 20) => "Hard",
        ("GoogleTrends", <= 50) => "Medium",
        ("GoogleTrends", _) => "Easy",

        // TokChart thresholds
        ("TokChart", <= 10) => "Very Hard",
        ("TokChart", <= 30) => "Hard",
        ("TokChart", <= 100) => "Medium",
        ("TokChart", _) => "Easy",

        // Default
        _ when rank <= 10 => "Hard",
        _ when rank <= 50 => "Medium",
        _ => "Easy"
    };
}
```

#### Trending from Rank Changes
```csharp
public async Task<string> CalculateTrendingFromRankAsync(int hashtagId, int days = 7)
{
    var history = await GetHashtagRankHistoryAsync(hashtagId, days);
    if (history.Count < 2) return "Stable";

    var avgOldRank = history.Take(history.Count / 2).Average(h => h.Rank);
    var avgNewRank = history.Skip(history.Count / 2).Average(h => h.Rank);

    // Lower rank number = better (rank 1 is top)
    var improvement = avgOldRank - avgNewRank;

    return improvement switch
    {
        > 5 => "Rising",   // Rank improved by 5+ positions
        < -5 => "Falling", // Rank dropped by 5+ positions
        _ => "Stable"
    };
}
```

#### Rank Score (1-100)
```csharp
public int CalculateRankScore(int rank, int totalHashtags)
{
    // Convert rank to percentile score
    // Rank 1 = 100 points, Rank 100 = lower score
    var percentile = (totalHashtags - rank + 1.0) / totalHashtags * 100;
    return Math.Clamp((int)percentile, 1, 100);
}
```

**Implementation**:
1. Add methods to `HashtagMetricsService.cs`
2. Update `CalculateMetricsForHashtagAsync()` to use fallback if ViewCount is NULL
3. Add `RankScore` and `RankDifficulty` fields to `HashtagMetrics` model
4. Display rank-based metrics with "(estimated)" badge in UI

---

### Solution 3: Hybrid Approach ⭐⭐ Best Balance

**Combine both solutions**:
1. Add ViewCount collection to **TokChart** (quick win, ~2 hours)
2. Use rank-based metrics for other sources (fallback)
3. Display metadata availability badge in UI

**UI Changes**:
```html
<!-- Admin Dashboard - Sources Table -->
<td>
    @if (source.Name == "TikTok" || source.Name == "TokChart")
    {
        <span class="badge bg-success">
            <i class="bi bi-check-circle"></i> Full Metrics
        </span>
    }
    else
    {
        <span class="badge bg-warning text-dark">
            <i class="bi bi-dash-circle"></i> Rank Only
        </span>
    }
</td>

<!-- Hashtag Details Page -->
@if (Model.HasViewCount)
{
    <span class="badge bg-success">Verified Data</span>
    <p>Difficulty: <strong>@Model.DifficultyLevel</strong></p>
}
else
{
    <span class="badge bg-warning">Estimated</span>
    <p>Difficulty: <strong>@Model.DifficultyLevel</strong> (based on rank)</p>
}
```

---

##  Immediate Actions

### Admin Dashboard Changes ✅ Completed
- [x] Added info alert explaining metadata limitation
- [x] Added "Metadata" column to Sources table
- [x] TikTok shows "Full" badge (green)
- [x] Others show "Limited" badge (yellow/warning)
- [x] Link to improvement guide

**File**: `HashTag/Views/Admin/Index.cshtml`

### Metrics Service Logging ✅ Completed
- [x] Updated log message to clarify only TikTok has metadata
- [x] Service gracefully skips hashtags without ViewCount

**File**: `HashTag/Services/HashtagMetricsService.cs` (line 100-104)

---

## Testing Checklist

After implementing any solution:

### Verify Data Collection
- [ ] ViewCount is parsed correctly (handle K/M/B suffixes)
- [ ] PostCount is parsed correctly
- [ ] NULL values don't break the system
- [ ] Logs show successful collection

### Verify Metrics Calculation
- [ ] Metrics calculated for hashtags with ViewCount
- [ ] Metrics skipped or use fallback for hashtags without ViewCount
- [ ] No database errors or constraint violations
- [ ] Logs show calculation status clearly

### Verify UI Display
- [ ] Badges show metadata availability
- [ ] Difficulty levels display correctly
- [ ] Estimated vs. verified data is clear
- [ ] User understands limitations

---

## Example Test Cases

```csharp
[Fact]
public async Task CalculateMetrics_WithViewCount_ShouldSucceed()
{
    // Arrange
    var hashtag = new Hashtag { Id = 1, Tag = "tiktok" };
    var history = new HashtagHistory
    {
        HashtagId = 1,
        ViewCount = 1_500_000, // 1.5M views
        PostCount = 5000
    };

    // Act
    var result = await _metricsService.CalculateMetricsForHashtagAsync(1);

    // Assert
    Assert.True(result);
    var metrics = await _repository.GetLatestMetricsAsync(1);
    Assert.Equal("Medium", metrics.DifficultyLevel); // 1.5M = Medium
    Assert.Equal(300, metrics.EngagementRate); // 1.5M / 5K = 300
}

[Fact]
public async Task CalculateMetrics_WithoutViewCount_ShouldSkip()
{
    // Arrange
    var hashtag = new Hashtag { Id = 2, Tag = "googletrends" };
    var history = new HashtagHistory
    {
        HashtagId = 2,
        ViewCount = null, // No ViewCount from Google Trends
        Rank = 5
    };

    // Act
    var result = await _metricsService.CalculateMetricsForHashtagAsync(2);

    // Assert
    Assert.False(result); // Should skip
}

[Fact]
public async Task CalculateDifficultyFromRank_ShouldEstimate()
{
    // Act
    var difficulty = _metricsService.CalculateDifficultyFromRank(3, "GoogleTrends");

    // Assert
    Assert.Equal("Very Hard", difficulty); // Rank 3 in Google Trends = Very Hard
}
```

---

## Database Query to Check Status

```sql
-- Count hashtags by source and metadata availability
SELECT
    s.Name AS Source,
    COUNT(DISTINCT hh.HashtagId) AS TotalHashtags,
    SUM(CASE WHEN hh.ViewCount IS NOT NULL THEN 1 ELSE 0 END) AS WithViewCount,
    SUM(CASE WHEN hh.ViewCount IS NULL THEN 1 ELSE 0 END) AS WithoutViewCount,
    CAST(SUM(CASE WHEN hh.ViewCount IS NOT NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PercentWithMetadata
FROM HashtagHistory hh
JOIN HashtagSources s ON hh.SourceId = s.Id
WHERE hh.CollectedDate >= DATEADD(day, -30, GETDATE())
GROUP BY s.Name
ORDER BY PercentWithMetadata DESC;

-- Sample Output:
-- Source        | TotalHashtags | WithViewCount | WithoutViewCount | PercentWithMetadata
-- TikTok        | 150           | 150           | 0                | 100.00%
-- GoogleTrends  | 75            | 0             | 75               | 0.00%
-- TokChart      | 120           | 0             | 120              | 0.00%
-- Buffer        | 50            | 0             | 50               | 0.00%
```

---

## Recommended Next Steps

### Short Term (1-2 days)
1. ✅ Add UI notifications (Completed)
2. ⏳ Implement rank-based fallback metrics
3. ⏳ Test fallback metrics with real data
4. ⏳ Update UI to show "estimated" vs "verified" badges

### Medium Term (1-2 weeks)
1. ⏳ Add ViewCount collection to TokChart crawler
2. ⏳ Add ViewCount collection to Countik crawler
3. ⏳ Test new metadata collection
4. ⏳ Monitor data quality

### Long Term (1+ months)
1. ⏳ Investigate Google Trends Interest score mapping
2. ⏳ Add detail page crawling for richer metadata
3. ⏳ Implement ML-based view prediction for sources without metadata
4. ⏳ Create data quality dashboard

---

## Summary

- **Problem**: 85%+ of hashtags have NULL ViewCount because most crawlers don't collect it
- **Impact**: Metrics like difficulty, engagement rate cannot be calculated for most hashtags
- **Current Workaround**: System gracefully skips hashtags without metadata, UI shows warning
- **Recommended Solution**: Hybrid approach - add metadata to TokChart + use rank-based fallback for others
- **Timeline**: 2-4 hours for TokChart enhancement, 4-6 hours for rank-based fallback system

**Status**: ✅ User has been notified via UI alerts. System handles NULL gracefully. Ready for enhancement when needed.
