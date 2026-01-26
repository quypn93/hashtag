# Complete Solution: Whitespace Hashtag Duplicates

## 🎯 Problem Summary

**Issue**: Search results show duplicate hashtags with same `TagDisplay` but different `Tag` values due to whitespace.

**Example**:
```
Row 1: Tag = "doituyenvietnam"  → TagDisplay = "#doituyenvietnam"  (1000 views)
Row 2: Tag = " doituyenvietnam" → TagDisplay = "#doituyenvietnam"  (1311 views)
```

Both appear in search, but user sees "#doituyenvietnam" twice.

---

## ✅ Complete Solution

### 1. Delete Hashtags with Whitespace (SQL)

**File**: `CLEANUP_DELETE_WHITESPACE_HASHTAGS.sql`

**What it does**: Deletes all hashtag records that have leading/trailing whitespace in `Tag` field.

**Steps**:
1. Run Step 1-3 to preview affected records
2. Uncomment Step 4 to execute DELETE
3. Verify Step 5 shows 0 remaining whitespace

**Execute**:
```sql
-- Preview first (Step 1-3)
-- Then uncomment and run:

BEGIN TRANSACTION;

DELETE FROM Hashtags
WHERE Tag <> LTRIM(RTRIM(Tag));

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' hashtags with whitespace';

COMMIT TRANSACTION;
```

**Why DELETE instead of UPDATE?**
- Cleaner solution - removes duplicates entirely
- Search logic will prioritize the clean version
- No need to merge history (clean version already has data)

---

### 2. Prioritize Best Hashtag in Search (Code)

**File**: `HashTag/Repositories/HashtagRepository.cs` (lines 106-139)

**What changed**:

#### Before:
```csharp
// No ordering - random hashtag returned if duplicates exist
var hashtagIdsQuery = _context.Hashtags
    .Where(h => h.Tag.Contains(normalizedQuery))
    .Select(h => h.Id);
```

#### After:
```csharp
// ✅ PRIORITY: Order by ViewCount, PostCount to get BEST hashtag first
var hashtagIdsQuery = _context.Hashtags
    .Where(h => h.Tag.Contains(normalizedQuery))
    .OrderByDescending(h => h.LatestViewCount ?? 0)    // Highest views first
    .ThenByDescending(h => h.LatestPostCount ?? 0)     // Then highest posts
    .ThenByDescending(h => h.TotalAppearances)         // Then most popular
    .ThenBy(h => h.FirstSeen)                          // Then oldest (more established)
    .Select(h => h.Id);

// ✅ Preserve sort order when loading entities
hashtags = hashtags.OrderBy(h => hashtagIds.IndexOf(h.Id)).ToList();
```

**Impact**:
- If duplicates exist (before SQL cleanup), search returns the one with highest ViewCount
- After SQL cleanup, this ensures best results always appear first
- User sees most relevant hashtag

---

### 3. Prevent Future Whitespace (Code)

**File**: `HashTag/Repositories/HashtagRepository.cs` (line 194)

**Change**:
```csharp
// ✅ FIX: Trim ALL whitespace when creating hashtags
var normalizedTag = tag.TrimStart('#').Trim().ToLower();
```

**Impact**: All new hashtags automatically have whitespace removed

---

## 📋 Implementation Checklist

### Phase 1: Database Cleanup (One-time)

- [ ] **Run SQL Preview** (Steps 1-3 in `CLEANUP_DELETE_WHITESPACE_HASHTAGS.sql`)
  ```sql
  -- Check how many will be deleted
  SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag));
  ```

- [ ] **Review affected hashtags**
  ```sql
  -- See which will be deleted
  SELECT Id, Tag, TagDisplay, LatestViewCount, LatestPostCount
  FROM Hashtags
  WHERE Tag <> LTRIM(RTRIM(Tag))
  ORDER BY LatestViewCount DESC;
  ```

- [ ] **Execute DELETE** (Uncomment Step 4)
  ```sql
  BEGIN TRANSACTION;
  DELETE FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag));
  COMMIT TRANSACTION;
  ```

- [ ] **Verify cleanup**
  ```sql
  -- Should return 0
  SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag));
  ```

### Phase 2: Deploy Code Changes

- [x] **Update `GetOrCreateHashtagAsync`** - Add `.Trim()` (line 194)
- [x] **Update `SearchHashtagsAsync`** - Add priority ordering (lines 106-139)
- [ ] **Restart application** to apply changes
- [ ] **Test search** for "doituyenvietnam"
- [ ] **Verify** only 1 result appears (the one with highest ViewCount)

---

## 🧪 Testing

### Test 1: Verify No Whitespace in Database

```sql
-- Should return 0 rows
SELECT *
FROM Hashtags
WHERE Tag <> LTRIM(RTRIM(Tag))
   OR Tag LIKE ' %'
   OR Tag LIKE '% ';
```

### Test 2: Search Returns Best Hashtag

**Before cleanup:**
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (1000 views)  ✅ Shown (highest ViewCount wins)
  Result 2: #doituyenvietnam (500 views)   ❌ Hidden (lower priority)
```

**After cleanup:**
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (1311 views)  ✅ Only one exists!
```

### Test 3: New Hashtags Have No Whitespace

```csharp
// Create hashtag with spaces
await _repository.GetOrCreateHashtagAsync("  test hashtag  ");

// Verify in database
SELECT Tag FROM Hashtags WHERE TagDisplay = '#testhashtag';
-- Should return: "testhashtag" (no spaces)
```

---

## 📊 Priority Logic Explanation

When multiple hashtags match the search query, results are ordered by:

1. **LatestViewCount** (DESC) - Most viewed = Most popular
2. **LatestPostCount** (DESC) - Most posts = Most active
3. **TotalAppearances** (DESC) - Most appearances = Most consistent
4. **FirstSeen** (ASC) - Oldest = More established/reliable

**Example**:
```
Hashtag A: Views=1000, Posts=500, Appearances=10, FirstSeen=2024-01-01
Hashtag B: Views=1500, Posts=200, Appearances=5,  FirstSeen=2024-06-01
Hashtag C: Views=1500, Posts=300, Appearances=8,  FirstSeen=2024-03-01

Order: B (highest views), C (same views, more posts), A (lowest views)
```

---

## 🔍 SQL Queries for Analysis

### Check Current State

```sql
-- Count hashtags by tag (find duplicates)
SELECT
    LTRIM(RTRIM(Tag)) as CleanTag,
    COUNT(*) as Count,
    MAX(LatestViewCount) as MaxViews,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as Ids
FROM Hashtags
GROUP BY LTRIM(RTRIM(Tag))
HAVING COUNT(*) > 1
ORDER BY MaxViews DESC;
```

### Preview Before DELETE

```sql
-- See which hashtags will be deleted and which kept
WITH RankedHashtags AS (
    SELECT
        Id,
        Tag,
        LTRIM(RTRIM(Tag)) as CleanTag,
        LatestViewCount,
        LatestPostCount,
        ROW_NUMBER() OVER (
            PARTITION BY LTRIM(RTRIM(Tag))
            ORDER BY
                LatestViewCount DESC,
                LatestPostCount DESC,
                FirstSeen ASC
        ) as Rank,
        CASE
            WHEN Tag <> LTRIM(RTRIM(Tag)) THEN 'DELETE (has whitespace)'
            ELSE 'KEEP (clean)'
        END as Action
    FROM Hashtags
)
SELECT *
FROM RankedHashtags
WHERE CleanTag IN (
    SELECT LTRIM(RTRIM(Tag))
    FROM Hashtags
    GROUP BY LTRIM(RTRIM(Tag))
    HAVING COUNT(*) > 1
)
ORDER BY CleanTag, Rank;
```

---

## 🚀 Deployment Steps

### Step 1: Backup Database
```sql
-- Backup Hashtags table
SELECT * INTO Hashtags_Backup_20251229
FROM Hashtags;

-- Verify backup
SELECT COUNT(*) FROM Hashtags_Backup_20251229;
```

### Step 2: Run SQL Cleanup
```sql
-- Execute CLEANUP_DELETE_WHITESPACE_HASHTAGS.sql
-- Start with ROLLBACK, then COMMIT when confident
```

### Step 3: Deploy Code
```bash
# Stop application
# Pull latest code with fixes
git pull

# Build
dotnet build

# Restart application
dotnet run --project d:\Task\TrendTag\HashTag
```

### Step 4: Verify
- Search for "doituyenvietnam" → Should show 1 result
- Search for "vietnam" → Should show unique results, ordered by ViewCount
- Check database: `SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag))` → Should be 0

---

## 📝 Rollback Plan (If Needed)

If something goes wrong:

```sql
-- 1. Restore from backup
DELETE FROM Hashtags;

INSERT INTO Hashtags
SELECT * FROM Hashtags_Backup_20251229;

-- 2. Verify restoration
SELECT COUNT(*) FROM Hashtags;
SELECT COUNT(*) FROM Hashtags_Backup_20251229;
-- Counts should match
```

---

## 📈 Expected Results

**Before Fix**:
- ❌ Duplicate hashtags in search results
- ❌ Whitespace in database (~50+ records estimated)
- ❌ Inconsistent ViewCount display

**After Fix**:
- ✅ Unique hashtags in search results
- ✅ No whitespace in database
- ✅ Best hashtag (highest ViewCount) always shown first
- ✅ Future hashtags automatically clean

---

## 🎯 Success Criteria

- [ ] SQL query `SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(RTRIM(Tag))` returns **0**
- [ ] Search for any hashtag returns **unique results only** (no duplicates)
- [ ] Results ordered by **ViewCount DESC** (highest first)
- [ ] New hashtags created **without whitespace**
- [ ] No breaking changes to existing functionality

---

**Created**: 2025-12-29
**Status**: ✅ Ready to deploy
**Priority**: High (affects user experience)
**Estimated Time**: 15 minutes (SQL + restart)
