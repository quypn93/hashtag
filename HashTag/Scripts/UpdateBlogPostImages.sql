-- Script to update FeaturedImage for blog posts
-- Run this script to add featured images to existing blog posts

USE TrendTagDb;
GO

-- Update "Top 100 Hashtag TikTok Trending Tháng 12/2025"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=800&q=80'
WHERE Slug = 'top-100-hashtag-tiktok-trending-thang-12-2025';

-- Update "5 Chiến Lược Hashtag TikTok Giúp Tăng Views Nhanh Chóng"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=800&q=80'
WHERE Slug = '5-chien-luoc-hashtag-tiktok-tang-views';

-- Update "Cách Tìm Hashtag TikTok Trending Theo Từng Ngành"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=800&q=80'
WHERE Slug = 'cach-tim-hashtag-tiktok-trending-theo-nganh';

-- Update "Top 10 Sai Lầm Khi Sử Dụng Hashtag TikTok"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162616475-46b635cb6868?w=800&q=80'
WHERE Slug = 'top-10-sai-lam-khi-su-dung-hashtag-tiktok';

-- Verify updates
SELECT
    Id,
    Title,
    Slug,
    FeaturedImage,
    Status
FROM BlogPosts
WHERE Slug IN (
    'top-100-hashtag-tiktok-trending-thang-12-2025',
    '5-chien-luoc-hashtag-tiktok-tang-views',
    'cach-tim-hashtag-tiktok-trending-theo-nganh',
    'top-10-sai-lam-khi-su-dung-hashtag-tiktok'
);

PRINT 'Blog post images updated successfully!';
GO
