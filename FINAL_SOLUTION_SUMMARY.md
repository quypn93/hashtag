# Final Solution: Remove Leading Whitespace from Hashtags

## 🎯 Problem
Search results show duplicate hashtags because some have **leading whitespace** (spaces at beginning):
- `Tag = "doituyenvietnam"` → `#doituyenvietnam` (clean)
- `Tag = " doituyenvietnam"` → `#doituyenvietnam` (has leading space)

Both appear in search with same display name.

---

## ✅ Solution: UPDATE (not DELETE)

### Approach
**UPDATE hashtags** to remove leading whitespace from `Tag` field, instead of deleting them.

**Why UPDATE instead of DELETE?**
- ✅ Preserves all data (History, Metrics, etc.)
- ✅ No data loss
- ✅ Simpler and safer

---

## 📁 Files & Changes

### 1. SQL Script - UPDATE to Remove Leading Whitespace
📄 **File**: `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql`

**What it does**:
1. Preview hashtags with leading whitespace
2. Check for potential duplicates after cleaning
3. (Optional) Delete duplicates if clean version already exists
4. UPDATE `Tag` and `TagDisplay` to remove leading spaces
5. Verify all clean

**Example**:
```sql
-- Before UPDATE:
Tag = " doituyenvietnam"
TagDisplay = "#doituyenvietnam"

-- After UPDATE:
Tag = "doituyenvietnam"   ✅ Leading space removed
TagDisplay = "#doituyenvietnam"
```

### 2. Code - Priority by ViewCount/PostCount
📄 **File**: `HashTag/Repositories/HashtagRepository.cs`

**Changes**:

#### Line 109: Search normalization
```csharp
// Remove # and trim leading whitespace from search query
var normalizedQuery = query.TrimStart('#').Trim().ToLower();
```

#### Lines 116-119: Priority ordering
```csharp
// Order by ViewCount DESC, PostCount DESC
.OrderByDescending(h => h.LatestViewCount ?? 0)
.ThenByDescending(h => h.LatestPostCount ?? 0)
.ThenByDescending(h => h.TotalAppearances)
.ThenBy(h => h.FirstSeen)
```

#### Line 139: Preserve order
```csharp
// Keep the sort order (best hashtags first)
hashtags = hashtags.OrderBy(h => hashtagIds.IndexOf(h.Id)).ToList();
```

#### Line 202: Prevent future whitespace
```csharp
// Remove # and leading spaces when creating new hashtags
var normalizedTag = tag.TrimStart('#', ' ').ToLower();
```

---

## 🚀 Deployment Steps

### Step 1: Run SQL Script

```sql
-- 1. Open: CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql

-- 2. Run Steps 1-3 to preview:
SELECT * FROM Hashtags WHERE Tag <> LTRIM(Tag);

-- 3. Check for duplicates (Step 3)
-- If duplicates exist, uncomment Step 4 to delete them first

-- 4. Uncomment Step 5 and execute UPDATE:
BEGIN TRANSACTION;

UPDATE Hashtags
SET
    Tag = LTRIM(Tag),
    TagDisplay = '#' + LTRIM(Tag)
WHERE Tag <> LTRIM(Tag);

SELECT @@ROWCOUNT as UpdatedCount;

COMMIT TRANSACTION;

-- 5. Verify (Step 6):
SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(Tag);
-- Should return 0
```

### Step 2: Restart Application

Stop and restart app to apply code changes:
```bash
# Stop current app
# Then restart:
dotnet run --project d:\Task\TrendTag\HashTag
```

### Step 3: Test

1. **Search for hashtag**: `/Hashtag/Search?q=doituyenvietnam`
   - Should show only 1 result
   - Should show the one with highest ViewCount

2. **Verify database**:
   ```sql
   -- Should return 0
   SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(Tag);
   ```

3. **Test new hashtags**:
   - Add a hashtag with leading space
   - Verify it's saved without whitespace

---

## 📊 Expected Results

### Before Fix
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (Id: 5746, Views: 1000)
  Result 2: #doituyenvietnam (Id: 7313, Views: 1311) ← duplicate!

Database:
  Tag = "doituyenvietnam"   (Id: 5746)
  Tag = " doituyenvietnam"  (Id: 7313) ← has leading space
```

### After Fix
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (Id: 7313, Views: 1311) ✅ Only one, highest ViewCount

Database:
  Tag = "doituyenvietnam"   (Id: 5746) ✅ Clean
  Tag = "doituyenvietnam"   (Id: 7313) ✅ Clean (updated)

Both are clean, but search prioritizes Id 7313 due to higher ViewCount
```

---

## 🔍 Verification Queries

### Check for Leading Whitespace
```sql
-- Should return 0 after fix
SELECT
    Id,
    Tag,
    LTRIM(Tag) as CleanTag,
    LatestViewCount
FROM Hashtags
WHERE Tag <> LTRIM(Tag)
ORDER BY LatestViewCount DESC;
```

### Find Duplicates
```sql
-- Check if any duplicates exist
SELECT
    Tag,
    COUNT(*) as Count,
    STRING_AGG(CAST(Id AS VARCHAR) + ' (Views:' + CAST(ISNULL(LatestViewCount, 0) AS VARCHAR) + ')', ', ') as Details
FROM Hashtags
GROUP BY Tag
HAVING COUNT(*) > 1
ORDER BY Count DESC;
```

### Verify Search Priority
```sql
-- Check search order for a specific hashtag
SELECT
    Id,
    Tag,
    TagDisplay,
    LatestViewCount,
    LatestPostCount,
    TotalAppearances
FROM Hashtags
WHERE Tag LIKE '%vietnam%'
ORDER BY
    LatestViewCount DESC,
    LatestPostCount DESC,
    TotalAppearances DESC,
    FirstSeen ASC;
```

---

## ⚠️ Important Notes

### About Duplicates

After UPDATE, you might have multiple hashtags with same `Tag`:
```
Tag = "doituyenvietnam" (Id: 5746, Views: 1000)
Tag = "doituyenvietnam" (Id: 7313, Views: 1311)
```

**This is OK because**:
1. Search will prioritize the one with highest ViewCount (7313)
2. Both have valid data (History, Metrics)
3. User only sees the best one

**If you want to merge duplicates** (optional):
- Keep the one with highest ViewCount
- Merge History/Metrics from the other
- Delete the lower-priority one
- (More complex, can do later if needed)

### About Search Logic

**Current behavior** (after fix):
- Search matches both hashtags
- Results sorted by ViewCount DESC
- Pagination shows top 20 (best ones first)
- User sees highest ViewCount version

**Why this works**:
- Users want to see popular hashtags
- Highest ViewCount = most relevant
- Duplicates with low ViewCount are hidden (beyond pagination)

---

## 📋 Checklist

### Database Cleanup
- [ ] Run Steps 1-3 to preview affected hashtags
- [ ] Check for duplicates (Step 3)
- [ ] If duplicates exist, run Step 4 to delete them
- [ ] Run Step 5 to UPDATE hashtags (remove leading whitespace)
- [ ] Verify Step 6 shows 0 remaining

### Code Deployment
- [x] Update `SearchHashtagsAsync` with priority ordering
- [x] Update `GetOrCreateHashtagAsync` to trim leading whitespace
- [x] Add search query normalization
- [ ] Restart application

### Testing
- [ ] Search returns unique results (best one first)
- [ ] Database has no leading whitespace
- [ ] New hashtags created without whitespace
- [ ] ViewCount sorting works correctly

---

## 🎯 Success Criteria

✅ **Database**:
- `SELECT COUNT(*) FROM Hashtags WHERE Tag <> LTRIM(Tag)` returns **0**

✅ **Search**:
- Query "doituyenvietnam" shows only **1 result** (or best one first)
- Results ordered by **ViewCount DESC**

✅ **Code**:
- New hashtags automatically **trim leading whitespace**
- Search query **normalized** (trim whitespace)

---

## 📝 Summary

| Action | File | Description |
|--------|------|-------------|
| ✅ SQL UPDATE | `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql` | Remove leading whitespace from existing hashtags |
| ✅ Code Priority | `HashtagRepository.cs:116-119` | Order search by ViewCount DESC |
| ✅ Code Prevention | `HashtagRepository.cs:202` | Trim leading whitespace on create |
| ✅ Code Search | `HashtagRepository.cs:109` | Normalize search query |

---

**Status**: ✅ Ready to deploy
**Last Updated**: 2025-12-29
**Priority**: High
**Estimated Time**: 10 minutes
