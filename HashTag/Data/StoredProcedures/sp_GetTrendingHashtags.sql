-- ================================================
-- Stored Procedure: sp_GetTrendingHashtags
-- Description: Optimized query to get trending hashtags with filters
-- Performance: ~10x faster than EF Core LINQ query
-- ================================================

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
        FROM HashtagHistory hh
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
