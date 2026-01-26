-- ================================================
-- Stored Procedure: sp_GetRecentBlogPosts
-- Description: Get most recent published blog posts
-- Performance: Simple query with efficient indexing
-- ================================================

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
