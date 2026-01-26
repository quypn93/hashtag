-- =====================================================
-- Add Slug column to HashtagCategories table for SEO-friendly URLs
-- =====================================================

USE [TrendTagDb];
GO

-- Step 1: Add Slug column (nullable first)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HashtagCategories]') AND name = 'Slug')
BEGIN
    ALTER TABLE [dbo].[HashtagCategories]
    ADD Slug NVARCHAR(200) NULL;

    PRINT 'Added Slug column to HashtagCategories table';
END
ELSE
BEGIN
    PRINT 'Slug column already exists in HashtagCategories table';
END
GO

-- Step 2: Populate Slug values based on DisplayNameVi (Vietnamese)
-- Use DisplayNameVi first, fallback to Name if DisplayNameVi is null
-- Convert Vietnamese to non-accented slug format

-- For now, use simple English slugs from Name field (will update to Vietnamese manually if needed)
UPDATE [dbo].[HashtagCategories]
SET Slug = LOWER(REPLACE(REPLACE(REPLACE(Name, ' & ', '-'), '&', ''), ' ', '-'))
WHERE Slug IS NULL;

PRINT 'Updated Slug values for all categories';

-- Manual mapping for Vietnamese slugs (optional - can run this after)
-- You can update these manually to Vietnamese if needed:
-- UPDATE HashtagCategories SET Slug = 'phuong-tien-giao-thong' WHERE Name = 'Vehicle & Transportation';
-- UPDATE HashtagCategories SET Slug = 'giao-duc' WHERE Name = 'Education';
-- UPDATE HashtagCategories SET Slug = 'cong-nghe-dien-tu' WHERE Name = 'Tech & Electronics';
-- UPDATE HashtagCategories SET Slug = 'lam-dep-cham-soc-ca-nhan' WHERE Name = 'Beauty & Personal Care';
-- UPDATE HashtagCategories SET Slug = 'thoi-trang-phu-kien' WHERE Name = 'Apparel & Accessories';
-- UPDATE HashtagCategories SET Slug = 'do-gia-dung' WHERE Name = 'Household Products';
-- UPDATE HashtagCategories SET Slug = 'thu-cung' WHERE Name = 'Pets';
-- UPDATE HashtagCategories SET Slug = 'cai-thien-nha-o' WHERE Name = 'Home Improvement';
-- UPDATE HashtagCategories SET Slug = 'tin-tuc-giai-tri' WHERE Name = 'News & Entertainment';
-- UPDATE HashtagCategories SET Slug = 'tro-choi' WHERE Name = 'Games';
-- UPDATE HashtagCategories SET Slug = 'dich-vu-song' WHERE Name = 'Life Services';
-- UPDATE HashtagCategories SET Slug = 'thuc-pham-do-uong' WHERE Name = 'Food & Beverage';
-- UPDATE HashtagCategories SET Slug = 'the-thao-ngoai-troi' WHERE Name = 'Sports & Outdoor';
-- UPDATE HashtagCategories SET Slug = 'du-lich' WHERE Name = 'Travel';
-- UPDATE HashtagCategories SET Slug = 'dich-vu-tai-chinh' WHERE Name = 'Financial Services';
-- UPDATE HashtagCategories SET Slug = 'em-be-tre-em-me-bau' WHERE Name = 'Baby, Kids & Maternity';
GO

-- Step 3: Verify the slugs
SELECT
    Id,
    Name,
    DisplayNameVi,
    Slug,
    IsActive
FROM [dbo].[HashtagCategories]
ORDER BY Name;
GO

-- =====================================================
-- EXAMPLE OUTPUT:
-- =====================================================
-- Id | Name                      | DisplayNameVi              | Slug                       | IsActive
-- ---|---------------------------|----------------------------|----------------------------|----------
-- 1  | Fashion                   | Thời Trang                 | fashion                    | 1
-- 2  | Tech                      | Công Nghệ                  | tech                       | 1
-- 3  | Vehicle & Transportation  | Phương Tiện & Giao Thông   | vehicle-&-transportation   | 1
-- etc.

-- =====================================================
-- NOTES:
-- =====================================================
-- 1. Slug is used for SEO-friendly URLs: /chu-de/{slug}
-- 2. Example: /chu-de/vehicle-&-transportation instead of /?categoryId=38
-- 3. The Category() action in HomeController will look up category by slug
-- 4. Special characters like & are preserved in slug (URL encoding will handle them)
