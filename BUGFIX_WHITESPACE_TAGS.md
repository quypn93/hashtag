# Bug Fix: Duplicate Hashtags Due to Whitespace

## 🐛 Problem Description

**Symptom**: Hashtags appear multiple times in search results with same `TagDisplay` but different `Tag` values

**Root Cause**: Crawled hashtags contain leading/trailing whitespace, creating multiple database records:
- `Tag = "doituyenvietnam"` → `TagDisplay = "#doituyenvietnam"`
- `Tag = " doituyenvietnam"` → `TagDisplay = "#doituyenvietnam"` (same display!)

**Database Evidence**:
```sql
SELECT Id, Tag, TagDisplay
FROM Hashtags
WHERE TagDisplay = '#doituyenvietnam';

-- Results:
-- 5746 | doituyenvietnam  | #doituyenvietnam
-- 7313 |  doituyenvietnam | #doituyenvietnam  (note leading space!)
```

---

## ✅ Solution

### 1. Fix Future Data (Prevent whitespace in new hashtags)

**File**: `HashTag/Repositories/HashtagRepository.cs` (line 194)

**Before**:
```csharp
var normalizedTag = tag.TrimStart('#').ToLower();
```

**After**:
```csharp
var normalizedTag = tag.TrimStart('#').Trim().ToLower(); // ✅ Added .Trim()
```

**Impact**: All new hashtags will have whitespace removed before saving

---

### 2. Clean Existing Data (One-time cleanup)

**File**: `CLEANUP_WHITESPACE_TAGS.sql`

**Steps**:

#### Step 1: Identify affected hashtags
```sql
SELECT
    LTRIM(RTRIM(Tag)) as CleanTag,
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as DuplicateIds
FROM Hashtags
GROUP BY LTRIM(RTRIM(Tag))
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;
```

#### Step 2: Update tags with whitespace
```sql
UPDATE Hashtags
SET
    Tag = LTRIM(RTRIM(Tag)),
    TagDisplay = '#' + LTRIM(RTRIM(Tag))
WHERE Tag <> LTRIM(RTRIM(Tag));
```

#### Step 3: Merge duplicate records (if needed)

**Option A: Keep older record, delete newer**
```sql
-- Find duplicates after cleaning
WITH Duplicates AS (
    SELECT
        Tag,
        MIN(Id) as KeepId,
        MIN(FirstSeen) as EarliestSeen
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
)
SELECT
    h.Id,
    h.Tag,
    h.FirstSeen,
    CASE WHEN h.Id = d.KeepId THEN 'KEEP' ELSE 'DELETE' END as Action
FROM Hashtags h
INNER JOIN Duplicates d ON h.Tag = d.Tag
ORDER BY h.Tag, h.Id;
```

**Option B: Merge history data then delete**
```sql
-- Move history from duplicates to main record
WITH Duplicates AS (
    SELECT Tag, MIN(Id) as KeepId
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
)
UPDATE HashtagHistory
SET HashtagId = d.KeepId
FROM HashtagHistory hh
INNER JOIN Hashtags h ON hh.HashtagId = h.Id
INNER JOIN Duplicates d ON h.Tag = d.Tag
WHERE h.Id <> d.KeepId;

-- Then delete duplicate hashtag records
DELETE h
FROM Hashtags h
INNER JOIN (
    SELECT Tag, MIN(Id) as KeepId
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
) d ON h.Tag = d.Tag
WHERE h.Id <> d.KeepId;
```

---

## 🧪 Testing

### Test 1: Verify no more whitespace in new hashtags

```csharp
[Test]
public async Task GetOrCreateHashtagAsync_TrimsWhitespace()
{
    // Arrange
    var repository = new HashtagRepository(context);

    // Act
    var hashtag = await repository.GetOrCreateHashtagAsync("  spaces  ");

    // Assert
    Assert.Equal("spaces", hashtag.Tag); // No whitespace
    Assert.Equal("#spaces", hashtag.TagDisplay);
}
```

### Test 2: Verify existing data is clean

```sql
-- Should return 0 rows
SELECT *
FROM Hashtags
WHERE Tag <> LTRIM(RTRIM(Tag))
   OR Tag LIKE ' %'
   OR Tag LIKE '% ';
```

### Test 3: Verify no duplicate TagDisplay

```sql
-- Should return 0 rows (no duplicates)
SELECT TagDisplay, COUNT(*)
FROM Hashtags
GROUP BY TagDisplay
HAVING COUNT(*) > 1;
```

---

## 📊 Impact Analysis

**Before Fix**:
- Hashtags with whitespace: ~50+ records (estimated)
- Duplicate TagDisplay values: ~25+ pairs
- Search results showing duplicates: Yes

**After Fix**:
- All new hashtags automatically trimmed: ✅
- Existing whitespace removed: ✅ (after running SQL)
- Duplicate records merged: ✅ (optional, if needed)
- Search results clean: ✅

---

## 🔍 Why This Happened

Possible sources of whitespace in hashtags:

1. **TikTok API responses**: May include extra spaces
2. **HTML parsing**: Whitespace from HTML elements
3. **Manual entry**: Admin adding hashtags manually
4. **Copy-paste**: Users copying hashtags with spaces

---

## 🚀 Prevention Strategy

### Code-level Prevention

✅ **Added .Trim() in repository** (line 194)
✅ **Validates input in controller** (if applicable)

### Additional Validation (Recommended)

Add validation attribute to `Hashtag` model:

```csharp
public class Hashtag
{
    [Required]
    [RegularExpression(@"^[^\s]+$", ErrorMessage = "Hashtag cannot contain whitespace")]
    public required string Tag { get; set; }

    // ... other properties
}
```

Add validation in crawler:

```csharp
// In CrawlerService or similar
private string NormalizeHashtag(string raw)
{
    return raw
        .TrimStart('#')
        .Trim()
        .Replace(" ", "") // Remove ALL spaces
        .ToLower();
}
```

---

## 📝 Migration Script (Complete Solution)

Save as: `MIGRATION_CleanupWhitespaceHashtags.sql`

```sql
BEGIN TRANSACTION;

-- Step 1: Update tags with whitespace
DECLARE @UpdatedCount INT;

UPDATE Hashtags
SET
    Tag = LTRIM(RTRIM(Tag)),
    TagDisplay = '#' + LTRIM(RTRIM(Tag))
WHERE Tag <> LTRIM(RTRIM(Tag));

SET @UpdatedCount = @@ROWCOUNT;
PRINT 'Updated ' + CAST(@UpdatedCount AS VARCHAR) + ' hashtags with whitespace';

-- Step 2: Merge duplicates (if any exist after cleanup)
DECLARE @MergedCount INT = 0;

WITH Duplicates AS (
    SELECT
        Tag,
        MIN(Id) as KeepId,
        MIN(FirstSeen) as EarliestFirstSeen
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
)
UPDATE h
SET
    FirstSeen = CASE WHEN d.EarliestFirstSeen < h.FirstSeen THEN d.EarliestFirstSeen ELSE h.FirstSeen END,
    TotalAppearances = (
        SELECT SUM(TotalAppearances)
        FROM Hashtags
        WHERE Tag = h.Tag
    )
FROM Hashtags h
INNER JOIN Duplicates d ON h.Tag = d.Tag AND h.Id = d.KeepId;

-- Move history from duplicates to kept record
UPDATE hh
SET HashtagId = d.KeepId
FROM HashtagHistory hh
INNER JOIN Hashtags h ON hh.HashtagId = h.Id
INNER JOIN (
    SELECT Tag, MIN(Id) as KeepId
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
) d ON h.Tag = d.Tag
WHERE h.Id <> d.KeepId;

SET @MergedCount = @@ROWCOUNT;
PRINT 'Merged history for ' + CAST(@MergedCount AS VARCHAR) + ' duplicate records';

-- Delete duplicate hashtags
DELETE h
FROM Hashtags h
INNER JOIN (
    SELECT Tag, MIN(Id) as KeepId
    FROM Hashtags
    GROUP BY Tag
    HAVING COUNT(*) > 1
) d ON h.Tag = d.Tag
WHERE h.Id <> d.KeepId;

DECLARE @DeletedCount INT = @@ROWCOUNT;
PRINT 'Deleted ' + CAST(@DeletedCount AS VARCHAR) + ' duplicate hashtag records';

-- Verify clean state
SELECT
    COUNT(*) as TotalHashtags,
    SUM(CASE WHEN Tag <> LTRIM(RTRIM(Tag)) THEN 1 ELSE 0 END) as WithWhitespace,
    COUNT(DISTINCT TagDisplay) as UniqueDisplayNames
FROM Hashtags;

-- COMMIT TRANSACTION;
-- Uncomment above after verifying results look correct
ROLLBACK TRANSACTION; -- Remove this line when ready to commit
```

---

## ✅ Verification Checklist

After running migration:

- [ ] No hashtags with whitespace: `SELECT * FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag))`
- [ ] No duplicate TagDisplay: `SELECT TagDisplay, COUNT(*) FROM Hashtags GROUP BY TagDisplay HAVING COUNT(*) > 1`
- [ ] Search returns unique results: Test `/Hashtag/Search?q=doituyenvietnam`
- [ ] History preserved: Verify old history records still attached to correct hashtags
- [ ] New hashtags clean: Add test hashtag with spaces, verify it's trimmed

---

**Fixed**: 2025-12-29
**Status**: ✅ Code fixed, SQL migration ready
**Severity**: Medium (affects search UX, no data loss)
**Priority**: High (common user interaction)
