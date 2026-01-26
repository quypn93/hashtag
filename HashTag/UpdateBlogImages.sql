-- Update FeaturedImage for existing blog posts
USE TrendTagDb;
GO

-- Update post 1: "Top 100 Hashtag TikTok Trending Tháng 12/2025"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80'
WHERE Slug = 'top-100-hashtag-tiktok-trending-thang-12-2025';

-- Update post 2: "5 Chiến Lược Hashtag TikTok Giúp Tăng Views Nhanh Chóng"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80'
WHERE Slug = '5-chien-luoc-hashtag-tiktok-tang-views';

-- Update post 3: "Cách Tìm Hashtag TikTok Trending Theo Từng Ngành"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80'
WHERE Slug = 'cach-tim-hashtag-tiktok-trending-theo-nganh';

-- Update post 4: "Top 10 Sai Lầm Khi Sử Dụng Hashtag TikTok"
UPDATE BlogPosts
SET FeaturedImage = 'https://images.unsplash.com/photo-1611162616475-46b635cb6868?w=1200&q=80'
WHERE Slug = 'top-10-sai-lam-khi-su-dung-hashtag-tiktok';

-- Verify updates
SELECT 
    Id, 
    Title, 
    Slug, 
    CASE 
        WHEN FeaturedImage IS NULL THEN 'No Image'
        ELSE 'Has Image'
    END AS ImageStatus,
    FeaturedImage
FROM BlogPosts
ORDER BY Id;

PRINT 'Featured images updated successfully!';
GO
