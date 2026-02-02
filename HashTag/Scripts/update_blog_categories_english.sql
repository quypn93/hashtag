-- Update BlogCategories with English names
-- Run this script to add English translations to existing categories

UPDATE BlogCategories
SET NameEn = 'Hashtag Trending',
    DescriptionEn = 'Analysis of the latest trending hashtags'
WHERE Slug = 'phan-tich-trending';

UPDATE BlogCategories
SET NameEn = 'TikTok Tips',
    DescriptionEn = 'Tips and strategies for TikTok growth'
WHERE Slug = 'meo-tiktok';

UPDATE BlogCategories
SET NameEn = 'Creator Guide',
    DescriptionEn = 'Guides for TikTok creators'
WHERE Slug = 'huong-dan-creator';

-- Verify the update
SELECT Id, Name, NameEn, DisplayNameVi, Slug, Description, DescriptionEn
FROM BlogCategories;
