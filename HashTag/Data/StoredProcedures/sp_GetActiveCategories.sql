-- ================================================
-- Stored Procedure: sp_GetActiveCategories
-- Description: Get all active hashtag categories with sub-categories
-- Performance: Simple query, but using SP for consistency
-- ================================================

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
