-- =====================================================
-- DELETE hashtags with LEADING whitespace (ONLY if duplicate TagDisplay exists)
-- Cannot UPDATE due to unique index on Tag field
-- Keep unique hashtags even if they have leading whitespace
-- =====================================================

-- Step 1: Preview hashtags that will be DELETED or KEPT
SELECT
    Id,
    Tag,
    LTRIM(Tag) as CleanTag,
    TagDisplay,
    LatestViewCount,
    LatestPostCount,
    TotalAppearances,
    FirstSeen,
    CASE
        WHEN Tag <> LTRIM(Tag) AND EXISTS (
            SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
            WHERE h2.TagDisplay = [TrendTagDb].[dbo].[Hashtags].TagDisplay
              AND h2.Id <> [TrendTagDb].[dbo].[Hashtags].Id
        ) THEN '❌ WILL DELETE (has leading whitespace + duplicate TagDisplay)'
        WHEN Tag <> LTRIM(Tag) THEN '✅ WILL KEEP (has leading whitespace but UNIQUE TagDisplay)'
        ELSE '✅ KEEP (clean)'
    END as Action,
    -- Check if duplicate TagDisplay exists
    CASE
        WHEN EXISTS (
            SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
            WHERE h2.TagDisplay = [TrendTagDb].[dbo].[Hashtags].TagDisplay
              AND h2.Id <> [TrendTagDb].[dbo].[Hashtags].Id
        ) THEN '⚠️ Duplicate TagDisplay EXISTS'
        ELSE '✅ Unique TagDisplay'
    END as DuplicateStatus
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag) -- Has leading whitespace
ORDER BY LTRIM(Tag), LatestViewCount DESC, LatestPostCount DESC;

-- Step 2: Count how many will be deleted vs kept
SELECT
    'Hashtags with LEADING whitespace + duplicate TagDisplay (will DELETE)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag)
  AND EXISTS (
      SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay AND h2.Id <> h1.Id
  )

UNION ALL

SELECT
    'Hashtags with LEADING whitespace but UNIQUE TagDisplay (will KEEP)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag)
  AND NOT EXISTS (
      SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay AND h2.Id <> h1.Id
  )

UNION ALL

SELECT
    'Total hashtags with LEADING whitespace' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- Step 3: Show duplicates by TagDisplay (what users see)
SELECT
    TagDisplay,
    COUNT(*) as TotalCount,
    STRING_AGG(
        CAST(Id AS VARCHAR) +
        ' (Tag:"' + Tag + '", Views:' + CAST(ISNULL(LatestViewCount, 0) AS VARCHAR) +
        ', Posts:' + CAST(ISNULL(LatestPostCount, 0) AS VARCHAR) +
        CASE WHEN Tag <> LTRIM(Tag) THEN ', HAS_WHITESPACE' ELSE '' END + ')',
        '; '
    ) as Details
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY TagDisplay
HAVING COUNT(*) > 1
ORDER BY MAX(LatestViewCount) DESC;

-- =====================================================
-- Step 4: DELETE hashtags with leading whitespace that have duplicate TagDisplay
-- =====================================================

-- This will DELETE hashtags with leading whitespace ONLY if another hashtag
-- with the same TagDisplay exists (to remove duplicates that users see)

/*
-- UNCOMMENT to execute:

BEGIN TRANSACTION;

-- Delete hashtags with leading whitespace that have duplicate TagDisplay
DELETE h1
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag) -- Has leading whitespace
  AND EXISTS (
      -- Check if another hashtag with same TagDisplay exists
      SELECT 1
      FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay
        AND h2.Id <> h1.Id
  );

DECLARE @DuplicatesDeleted INT = @@ROWCOUNT;
PRINT 'Deleted ' + CAST(@DuplicatesDeleted AS VARCHAR) + ' hashtags with leading whitespace (that have duplicate TagDisplay)';

-- Show what was kept (hashtags with leading whitespace but unique TagDisplay)
SELECT
    Id, Tag, TagDisplay, LatestViewCount,
    'KEPT (has leading whitespace but unique TagDisplay)' as Status
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- COMMIT TRANSACTION;
ROLLBACK TRANSACTION; -- Remove when ready to commit
*/

-- =====================================================
-- Step 5: Verify after deletion
-- =====================================================

-- Count hashtags with leading whitespace (may have some if they are unique)
SELECT
    'Hashtags with LEADING whitespace remaining (kept because unique TagDisplay)' as Status,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- Show hashtags with leading whitespace that were kept (because unique TagDisplay)
SELECT TOP 10
    Id,
    Tag,
    LTRIM(Tag) as CleanVersion,
    TagDisplay,
    LatestViewCount,
    'KEPT (unique TagDisplay)' as Reason
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag)
ORDER BY LatestViewCount DESC;

-- Verify no duplicate TagDisplay exists (THIS is what matters to users)
SELECT
    TagDisplay,
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR) + ' (Tag:"' + Tag + '")', ', ') as Details
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY TagDisplay
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;
-- Should return 0 rows (no duplicate TagDisplay)

-- =====================================================
-- SUMMARY OF WHAT THIS SCRIPT DOES:
-- =====================================================
-- 1. Preview hashtags with leading whitespace (shows which will DELETE vs KEEP)
-- 2. Count how many will be deleted vs kept
-- 3. Show duplicates by TagDisplay (what users see in search)
-- 4. Delete hashtags with leading whitespace ONLY if duplicate TagDisplay exists
-- 5. Verify no duplicate TagDisplay remains

-- WHY DELETE instead of UPDATE?
-- - Unique index IX_Hashtags_Tag prevents updating " tag" to "tag" if "tag" already exists
-- - Deleting whitespace versions is cleaner and avoids constraint violations

-- WHY only delete if duplicate TagDisplay exists?
-- - If hashtag with leading whitespace is UNIQUE (no other hashtag has same TagDisplay), keep it
-- - Only delete when there are duplicates (same TagDisplay shown to users)

-- EXAMPLE 1 - DUPLICATE (WILL DELETE):
-- Database has:
--   Id: 5746, Tag = "doituyenvietnam",  TagDisplay = "#doituyenvietnam" (1000 views) ✅ KEEP
--   Id: 7313, Tag = " doituyenvietnam", TagDisplay = "#doituyenvietnam" (1311 views) ❌ DELETE (duplicate TagDisplay)
--
-- After deletion:
--   Id: 5746, Tag = "doituyenvietnam",  TagDisplay = "#doituyenvietnam" (1000 views) ✅ Remains
--
-- Search will show the clean version (Id: 5746) since whitespace version is deleted

-- EXAMPLE 2 - UNIQUE (WILL KEEP):
-- Database has:
--   Id: 9999, Tag = " uniquetag", TagDisplay = "#uniquetag" (500 views) ✅ KEEP (no duplicate)
--
-- After deletion:
--   Id: 9999, Tag = " uniquetag", TagDisplay = "#uniquetag" (500 views) ✅ Still exists (kept because unique)
--
-- Note: This hashtag will still work in search, just has leading whitespace in Tag field
