# Crawlable Data Analysis - TrendTag

## Data Fields Available from Each Source

### 1. TikTok Creative Center ⭐ (RICHEST SOURCE)
**URL**: `https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag`

**Available Fields**:
- ✅ **Hashtag Name** (#example)
- ✅ **Rank** (1, 2, 3...)
- ✅ **Post Count** (222K Posts) → Can convert to number
- ✅ **View Count** (161,981,859 views) → Exact number
- ✅ **Industry/Category** (News & Entertainment, Fashion, etc.)
- ✅ **Trend Data** (7-day time series with trend scores 0-1)
- ✅ **Rank Change** (new, up, down, stable)
- ✅ **Featured Creators** (nicknames, avatars)
- ✅ **Hashtag ID** (internal TikTok ID)

**How to Extract**:
- Page loads data via JSON in `<script>` tags (dehydratedState)
- OR scrape table rows with Playwright
- Can extract from `data-testid` elements

**Extraction Difficulty**: Medium (need to parse JSON or complex selectors)

---

### 2. Buffer
**URL**: `https://buffer.com/resources/tiktok-hashtags/`

**Available Fields**:
- ✅ **Hashtag Name**
- ✅ **Category/Niche** (Beauty, Fashion, Food, etc.) - inferred from article sections
- ❌ No view counts
- ❌ No post counts
- ❌ No difficulty metrics

**How to Extract**:
- Hashtags grouped in sections with headers
- Can extract category from section title

**Extraction Difficulty**: Easy (just regex for hashtags + section headings)

---

### 3. Google Trends
**URL**: `https://trends.google.com/trends/trendingsearches/daily?geo=VN`

**Available Fields**:
- ✅ **Search Term** (converted to hashtag)
- ✅ **Rank/Position**
- ⚠️ **Search Volume** (sometimes shown, relative not absolute)
- ⚠️ **Related Queries** (sometimes available)
- ⚠️ **News Articles** (linked articles)
- ❌ No exact numbers usually

**How to Extract**:
- Table rows with search terms
- Some data in JSON feed

**Extraction Difficulty**: Medium (structure varies)

---

### 4. Trollishly
**URL**: `https://www.trollishly.com/tiktok-trending-hashtags/`

**Available Fields**:
- ✅ **Hashtag Name**
- ⚠️ **Category** (might be mentioned in text)
- ❌ No metrics

**Extraction Difficulty**: Easy (just hashtag names)

---

### 5. CapCut
**URL**: `https://www.capcut.com/resource/tiktok-hashtag-guide`

**Available Fields**:
- ✅ **Hashtag Name**
- ⚠️ **Use Case/Category** (from article context)
- ❌ No metrics

**Extraction Difficulty**: Easy

---

### 6. TokChart
**URL**: `https://tokchart.com/hashtags`

**Available Fields**:
- ✅ **Hashtag Name**
- ✅ **Possibly View Count** (if displayed in table)
- ✅ **Rank**
- ⚠️ May require authentication or have rate limits

**Extraction Difficulty**: Medium-High (might have anti-scraping)

---

### 7. Countik
**URL**: `https://countik.com/popular/hashtags`

**Available Fields**:
- ✅ **Hashtag Name**
- ✅ **Possibly View Count**
- ✅ **Rank**
- ⚠️ Structure unknown without testing

**Extraction Difficulty**: Medium

---

## Recommended Data Model Based on Analysis

### Core Metadata We CAN Crawl:

#### From TikTok Creative Center (PRIMARY SOURCE):
1. **ViewCount** (long) - Total video views for hashtag
2. **PostCount** (long) - Total posts using hashtag
3. **Category** (string) - Industry classification
4. **TrendScore** (decimal) - 0-1 score from 7-day data
5. **RankChange** (enum: New, Up, Down, Stable)
6. **FeaturedCreators** (JSON string) - Top creators using it

#### Calculated Fields:
7. **DifficultyLevel** (Easy/Medium/Hard) - Based on ViewCount + PostCount
8. **EngagementRate** (decimal) - ViewCount / PostCount ratio
9. **TrendingDirection** (enum: Rising, Falling, Stable)

#### From Other Sources (SUPPLEMENTARY):
10. **Niches** (string array) - Categories from Buffer/other sources
11. **RelatedKeywords** (string array) - For intent matching

---

## Proposed Enhanced Schema

### HashtagHistory (UPDATE)
```csharp
public class HashtagHistory
{
    public int Id { get; set; }
    public int HashtagId { get; set; }
    public int SourceId { get; set; }
    public int Rank { get; set; }
    public DateTime CollectedDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // NEW FIELDS
    public long? ViewCount { get; set; }              // Total views
    public long? PostCount { get; set; }               // Total posts
    public string? Category { get; set; }              // Industry
    public decimal? TrendScore { get; set; }           // 0-1 score
    public string? RankChange { get; set; }            // "new", "up", "down", "stable"
    public string? FeaturedCreatorsJson { get; set; }  // JSON array
}
```

### HashtagMetrics (NEW TABLE)
```csharp
public class HashtagMetrics
{
    public int Id { get; set; }
    public int HashtagId { get; set; }
    public DateTime CalculatedAt { get; set; }

    // CALCULATED METRICS
    public string DifficultyLevel { get; set; }       // "Easy", "Medium", "Hard"
    public decimal? EngagementRate { get; set; }      // Views/Posts ratio
    public string TrendingDirection { get; set; }     // "Rising", "Falling", "Stable"
    public decimal? GrowthRate7Days { get; set; }     // % change in 7 days
    public int? CompetitionScore { get; set; }        // 1-100

    // AGGREGATED DATA (from multiple sources)
    public long? AverageViewCount { get; set; }
    public long? AveragePostCount { get; set; }
}
```

### HashtagNiche (NEW TABLE - Many-to-Many)
```csharp
public class HashtagNiche
{
    public int Id { get; set; }
    public int HashtagId { get; set; }
    public string NicheName { get; set; }             // "Fashion", "Beauty", "Food"
    public string Source { get; set; }                 // Where this categorization came from
    public decimal Confidence { get; set; }            // 0-1 confidence score
}
```

---

## Implementation Priority

### Phase 1: Enhance TikTok Crawler (HIGHEST VALUE)
- Extract ViewCount, PostCount, Category, TrendScore
- Parse JSON data or use advanced selectors
- **Impact**: Enables difficulty calculation + intent matching

### Phase 2: Add Calculated Metrics
- Create HashtagMetrics table
- Implement calculation service
- Run nightly to update metrics
- **Impact**: Powers filtering by difficulty

### Phase 3: Add Category/Niche Extraction
- Extract from Buffer (easy)
- Create HashtagNiche table
- **Impact**: Better search relevance

### Phase 4: Add Keywords for Intent
- Extract related keywords
- Build keyword index
- **Impact**: "bán áo local brand" → finds #localbrand #fashion #smallbusiness

---

## Extraction Code Examples

### TikTok ViewCount & PostCount
```javascript
// In Playwright JavaScript evaluation
const hashtags = await page.evaluate(() => {
    const items = document.querySelectorAll('[data-testid^="cc_commonCom-trend_hashtag_item"]');
    return Array.from(items).map(item => {
        const name = item.querySelector('.titleText')?.textContent;
        const viewsText = item.querySelector('.video-views')?.textContent; // e.g., "161M"
        const postsText = item.querySelector('.publish-count')?.textContent; // e.g., "222K Posts"

        return {
            name,
            views: parseCount(viewsText),
            posts: parseCount(postsText)
        };
    });
});

function parseCount(text) {
    if (!text) return null;
    const match = text.match(/([0-9.]+)([KMB])?/);
    if (!match) return null;

    const num = parseFloat(match[1]);
    const suffix = match[2];

    const multipliers = { 'K': 1000, 'M': 1000000, 'B': 1000000000 };
    return Math.round(num * (multipliers[suffix] || 1));
}
```

---

## Next Steps

1. ✅ Update database schema (add migrations)
2. ✅ Enhance TikTok crawler to extract all available fields
3. ✅ Create metrics calculation service
4. ✅ Update search to filter by difficulty
5. ✅ Add intent-based keyword matching

**Estimated Impact**:
- Current: Only hashtag names + basic rank
- After: Full metadata for smart filtering, difficulty levels, trending detection, intent matching

This will enable the key user flow:
"bán áo local brand" → System finds easy-difficulty hashtags in fashion niche with rising trends
