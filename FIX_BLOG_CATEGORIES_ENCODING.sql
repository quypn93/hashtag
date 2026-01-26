-- =====================================================
-- Fix Blog Categories Encoding (UTF-8 Vietnamese)
-- =====================================================
-- Run this script if Vietnamese text displays incorrectly
-- =====================================================

USE [TrendTagDb];
GO

PRINT 'Fixing BlogCategories encoding...';

-- Update TikTok Tips
UPDATE BlogCategories SET
    DisplayNameVi = N'Mẹo TikTok',
    Description = N'Các mẹo và chiến lược để tăng view, follower trên TikTok'
WHERE Slug = 'tiktok-tips';

-- Update Hashtag Strategy
UPDATE BlogCategories SET
    DisplayNameVi = N'Chiến Lược Hashtag',
    Description = N'Hướng dẫn chọn và sử dụng hashtag hiệu quả'
WHERE Slug = 'hashtag-strategy';

-- Update Trending Analysis
UPDATE BlogCategories SET
    DisplayNameVi = N'Phân Tích Trending',
    Description = N'Phân tích các xu hướng viral trên TikTok'
WHERE Slug = 'trending-analysis';

-- Update Creator Guide
UPDATE BlogCategories SET
    DisplayNameVi = N'Hướng Dẫn Creator',
    Description = N'Hướng dẫn toàn diện cho TikTok creator'
WHERE Slug = 'creator-guide';

-- Update Case Studies
UPDATE BlogCategories SET
    DisplayNameVi = N'Nghiên Cứu Điển Hình',
    Description = N'Các case study thành công về tăng trưởng TikTok'
WHERE Slug = 'case-studies';

-- Update News & Updates
UPDATE BlogCategories SET
    DisplayNameVi = N'Tin Tức & Cập Nhật',
    Description = N'Tin tức mới nhất về TikTok và social media'
WHERE Slug = 'news-updates';

PRINT '✓ All BlogCategories updated with correct Vietnamese encoding';

-- Verify results
SELECT
    Id,
    Name,
    DisplayNameVi,
    Slug,
    Description,
    IsActive
FROM BlogCategories
ORDER BY Id;

PRINT '';
PRINT 'Encoding fix complete!';
PRINT 'Reload your application to see changes.';
GO
