-- ========================================
-- Clean up hashtags with leading/trailing whitespace
-- ========================================

-- Step 1: Find duplicate tags (after trimming)
SELECT
    LTRIM(RTRIM(Tag)) as CleanTag,
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as DuplicateIds
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY LTRIM(RTRIM(Tag))
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- Step 2: Preview what will be updated
SELECT
    Id,
    Tag,
    LTRIM(RTRIM(Tag)) as CleanedTag,
    TagDisplay,
    '#' + LTRIM(RTRIM(Tag)) as CleanedTagDisplay,
    CASE
        WHEN Tag <> LTRIM(RTRIM(Tag)) THEN 'Will Update'
        ELSE 'Already Clean'
    END as Status
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(RTRIM(Tag))
ORDER BY Tag;

-- Step 3: Update tags with whitespace (CAREFUL - Review Step 2 first!)
-- UNCOMMENT BELOW AFTER REVIEWING:
/*
UPDATE [TrendTagDb].[dbo].[Hashtags]
SET
    Tag = LTRIM(RTRIM(Tag)),
    TagDisplay = '#' + LTRIM(RTRIM(Tag))
WHERE Tag <> LTRIM(RTRIM(Tag));

-- Check how many were updated
SELECT @@ROWCOUNT as RowsUpdated;
*/

-- Step 4: Merge duplicate hashtags (if needed)
-- This requires more complex logic to merge History, Metrics, etc.
-- For now, we'll just identify them:
SELECT
    LTRIM(RTRIM(Tag)) as Tag,
    MIN(Id) as KeepId,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as MergeFromIds,
    COUNT(*) as TotalDuplicates
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY LTRIM(RTRIM(Tag))
HAVING COUNT(*) > 1;

-- ========================================
-- After cleaning, verify no more whitespace
-- ========================================
SELECT
    COUNT(*) as TotalHashtags,
    SUM(CASE WHEN Tag <> LTRIM(RTRIM(Tag)) THEN 1 ELSE 0 END) as WithWhitespace,
    SUM(CASE WHEN Tag = LTRIM(RTRIM(Tag)) THEN 1 ELSE 0 END) as Clean
FROM [TrendTagDb].[dbo].[Hashtags];
