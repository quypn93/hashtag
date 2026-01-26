-- ================================================
-- Deploy All Stored Procedures for TrendTag
-- Run this script to create/update all SPs at once
-- ================================================

USE TrendTag;
GO

PRINT 'Starting stored procedures deployment...';
GO

-- ================================================
-- 1. sp_GetTrendingHashtags
-- ================================================
PRINT 'Creating sp_GetTrendingHashtags...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetTrendingHashtags]
    @CategoryId INT = NULL,
    @DifficultyLevel NVARCHAR(50) = NULL,
    @SourceIds NVARCHAR(MAX) = NULL, -- Comma-separated source IDs: "1,2,3"
    @MinRank INT = NULL,
    @MaxRank INT = NULL,
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @SortBy NVARCHAR(50) = 'BestRank', -- BestRank, TotalAppearances, LastSeen, TrendMomentum
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    -- Create temp table for source IDs filtering
    DECLARE @SourceIdTable TABLE (SourceId INT);

    IF @SourceIds IS NOT NULL
    BEGIN
        INSERT INTO @SourceIdTable (SourceId)
        SELECT CAST(value AS INT)
        FROM STRING_SPLIT(@SourceIds, ',');
    END

    -- Main query with CTEs for better performance
    ;WITH LatestHistory AS (
        SELECT
            hh.HashtagId,
            hh.SourceId,
            hh.Rank,
            hh.ViewCount,
            hh.PostCount,
            hh.RankDiff,
            hh.IsViral,
            hh.TrendDataJson,
            hh.TrendMomentum,
            hh.Category,
            hh.CollectedDate,
            s.Name AS SourceName,
            ROW_NUMBER() OVER (PARTITION BY hh.HashtagId ORDER BY hh.CollectedDate DESC) AS rn
        FROM HashtagHistories hh
        INNER JOIN HashtagSources s ON hh.SourceId = s.Id
        WHERE
            (@CategoryId IS NULL OR EXISTS (
                SELECT 1 FROM Hashtags h2
                WHERE h2.Id = hh.HashtagId AND h2.CategoryId = @CategoryId
            ))
            AND (@SourceIds IS NULL OR hh.SourceId IN (SELECT SourceId FROM @SourceIdTable))
            AND (@MinRank IS NULL OR hh.Rank >= @MinRank)
            AND (@MaxRank IS NULL OR hh.Rank <= @MaxRank)
            AND (@StartDate IS NULL OR hh.CollectedDate >= @StartDate)
            AND (@EndDate IS NULL OR hh.CollectedDate <= @EndDate)
    )
    SELECT TOP (@Limit)
        h.Id,
        h.Tag,
        h.TagDisplay,
        ISNULL(lh.Rank, 999) AS BestRank,
        h.TotalAppearances,
        lh.SourceName AS Sources,
        h.LastSeen,
        h.DifficultyLevel,
        ISNULL(lh.Category, c.Name) AS CategoryName,
        lh.ViewCount AS LatestViewCount,
        lh.PostCount AS LatestPostCount,
        lh.RankDiff,
        ISNULL(lh.IsViral, 0) AS IsViral,
        lh.TrendDataJson,
        lh.TrendMomentum
    FROM Hashtags h
    INNER JOIN LatestHistory lh ON h.Id = lh.HashtagId AND lh.rn = 1
    LEFT JOIN HashtagCategories c ON h.CategoryId = c.Id
    WHERE
        h.IsActive = 1
        AND (@DifficultyLevel IS NULL OR h.DifficultyLevel = @DifficultyLevel)
    ORDER BY
        CASE
            WHEN @SortBy = 'TotalAppearances' THEN h.TotalAppearances
            WHEN @SortBy = 'TrendMomentum' THEN CAST(ISNULL(lh.TrendMomentum, -999) AS INT)
            ELSE 0
        END DESC,
        CASE
            WHEN @SortBy = 'LastSeen' THEN h.LastSeen
            ELSE NULL
        END DESC,
        CASE
            WHEN @SortBy = 'BestRank' OR @SortBy IS NULL THEN ISNULL(lh.Rank, 999)
            ELSE 999
        END ASC;

END
GO

PRINT 'sp_GetTrendingHashtags created successfully.';
GO

-- ================================================
-- 2. sp_GetActiveCategories
-- ================================================
PRINT 'Creating sp_GetActiveCategories...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetActiveCategories]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id,
        c.Name,
        c.DisplayNameVi,
        c.Slug,
        c.Icon,
        c.ParentCategoryId,
        c.IsActive
    FROM HashtagCategories c
    WHERE c.IsActive = 1
    ORDER BY c.Name;

END
GO

PRINT 'sp_GetActiveCategories created successfully.';
GO

-- ================================================
-- 3. sp_GetRecentBlogPosts
-- ================================================
PRINT 'Creating sp_GetRecentBlogPosts...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetRecentBlogPosts]
    @Count INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Count)
        bp.Id,
        bp.Title,
        bp.Slug,
        bp.Excerpt,
        bp.Content,
        bp.FeaturedImage,
        bp.PublishedAt,
        bp.UpdatedAt,
        bp.ViewCount,
        bp.MetaTitle,
        bp.MetaDescription,
        bp.MetaKeywords,
        bp.Status,
        c.Id AS CategoryId,
        c.Name AS CategoryName,
        c.Slug AS CategorySlug,
        c.DisplayNameVi AS CategoryDisplayNameVi
    FROM BlogPosts bp
    INNER JOIN BlogCategories c ON bp.CategoryId = c.Id
    WHERE
        bp.Status = 'Published'
        AND bp.PublishedAt IS NOT NULL
        AND bp.PublishedAt <= GETUTCDATE()
    ORDER BY bp.PublishedAt DESC;

END
GO

PRINT 'sp_GetRecentBlogPosts created successfully.';
GO

-- ================================================
-- Verify Deployment
-- ================================================
PRINT '';
PRINT 'Verifying deployment...';
GO

SELECT
    name AS [Stored Procedure],
    create_date AS [Created],
    modify_date AS [Modified]
FROM sys.procedures
WHERE name IN ('sp_GetTrendingHashtags', 'sp_GetActiveCategories', 'sp_GetRecentBlogPosts')
ORDER BY name;
GO

PRINT '';
PRINT '✅ All stored procedures deployed successfully!';
PRINT '';
PRINT 'You can now test them:';
PRINT '  EXEC sp_GetTrendingHashtags @Limit = 10;';
PRINT '  EXEC sp_GetActiveCategories;';
PRINT '  EXEC sp_GetRecentBlogPosts @Count = 5;';
GO
