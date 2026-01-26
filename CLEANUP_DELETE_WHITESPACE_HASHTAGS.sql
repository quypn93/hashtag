-- =====================================================
-- DELETE hashtags with LEADING whitespace (spaces at beginning)
-- Keep the best one (highest ViewCount/PostCount)
-- =====================================================

-- Step 1: Preview hashtags that will be DELETED (have leading whitespace)
SELECT
    Id,
    Tag,
    LTRIM(Tag) as CleanedTag,
    TagDisplay,
    LatestViewCount,
    LatestPostCount,
    TotalAppearances,
    FirstSeen,
    LastSeen,
    CASE
        WHEN Tag <> LTRIM(Tag) THEN '❌ WILL DELETE (has LEADING whitespace)'
        ELSE '✅ KEEP (clean)'
    END as Action
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag) -- Has leading whitespace only
ORDER BY LTRIM(Tag), LatestViewCount DESC, LatestPostCount DESC;

-- Step 2: Count how many will be affected
SELECT
    'Hashtags with LEADING whitespace (will DELETE)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag)

UNION ALL

SELECT
    'Clean hashtags (will KEEP)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag = LTRIM(Tag);

-- Step 3: Show potential duplicates AFTER cleaning (removing LEADING whitespace)
WITH CleanedTags AS (
    SELECT
        Id,
        LTRIM(Tag) as CleanTag,
        Tag as OriginalTag,
        LatestViewCount,
        LatestPostCount,
        TotalAppearances,
        FirstSeen
    FROM [TrendTagDb].[dbo].[Hashtags]
)
SELECT
    CleanTag,
    COUNT(*) as DuplicateCount,
    MAX(LatestViewCount) as MaxViewCount,
    MAX(LatestPostCount) as MaxPostCount,
    STRING_AGG(CAST(Id AS VARCHAR) + ' (Views:' + CAST(ISNULL(LatestViewCount, 0) AS VARCHAR) + ')', ', ') as AllIds
FROM CleanedTags
GROUP BY CleanTag
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, MaxViewCount DESC;

-- =====================================================
-- Step 4: EXECUTE DELETE (CAREFUL!)
-- Only deletes hashtags with LEADING whitespace
-- =====================================================

-- UNCOMMENT BELOW TO EXECUTE:
/*
BEGIN TRANSACTION;

-- Delete hashtags with LEADING whitespace only
DELETE FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- Check how many were deleted
DECLARE @DeletedCount INT = @@ROWCOUNT;
PRINT 'Deleted ' + CAST(@DeletedCount AS VARCHAR) + ' hashtags with LEADING whitespace';

-- Verify no more leading whitespace
SELECT
    COUNT(*) as TotalHashtags,
    SUM(CASE WHEN Tag <> LTRIM(Tag) THEN 1 ELSE 0 END) as WithLeadingWhitespace,
    SUM(CASE WHEN Tag = LTRIM(Tag) THEN 1 ELSE 0 END) as Clean
FROM [TrendTagDb].[dbo].[Hashtags];

-- COMMIT TRANSACTION;
ROLLBACK TRANSACTION; -- Remove this line when ready to commit
*/

-- =====================================================
-- Step 5: Verify after deletion
-- =====================================================

-- Should return 0 hashtags with LEADING whitespace
SELECT
    'Hashtags with LEADING whitespace remaining' as Status,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- Show examples if any remaining (should be empty)
SELECT TOP 10
    Id,
    Tag,
    LTRIM(Tag) as ShouldBe,
    TagDisplay,
    LatestViewCount
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag)
ORDER BY LatestViewCount DESC;

-- Check for any duplicates after cleanup
SELECT
    Tag,
    COUNT(*) as DuplicateCount,
    MAX(LatestViewCount) as MaxViewCount,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as Ids
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY Tag
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;
