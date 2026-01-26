-- =====================================================
-- Create Blog System Tables for TrendTag
-- =====================================================
-- Purpose: Enable blog/content marketing for SEO
-- Author: TrendTag Team
-- Date: 2025-12-30
-- =====================================================

USE [TrendTagDb];
GO

-- =====================================================
-- Table: BlogCategories
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogCategories')
BEGIN
    CREATE TABLE [dbo].[BlogCategories] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [DisplayNameVi] NVARCHAR(100) NULL,
        [Slug] NVARCHAR(150) NOT NULL UNIQUE,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,

        INDEX IX_BlogCategories_Slug (Slug),
        INDEX IX_BlogCategories_IsActive (IsActive)
    );

    PRINT 'Created BlogCategories table';
END
ELSE
BEGIN
    PRINT 'BlogCategories table already exists';
END
GO

-- =====================================================
-- Table: BlogTags
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogTags')
BEGIN
    CREATE TABLE [dbo].[BlogTags] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(50) NOT NULL,
        [Slug] NVARCHAR(70) NOT NULL UNIQUE,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        INDEX IX_BlogTags_Slug (Slug)
    );

    PRINT 'Created BlogTags table';
END
ELSE
BEGIN
    PRINT 'BlogTags table already exists';
END
GO

-- =====================================================
-- Table: BlogPosts
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogPosts')
BEGIN
    CREATE TABLE [dbo].[BlogPosts] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(250) NOT NULL UNIQUE,
        [Excerpt] NVARCHAR(500) NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [FeaturedImage] NVARCHAR(500) NULL,

        -- SEO Fields
        [MetaTitle] NVARCHAR(200) NULL,
        [MetaDescription] NVARCHAR(500) NULL,
        [MetaKeywords] NVARCHAR(500) NULL,

        -- Author & Category
        [Author] NVARCHAR(100) NOT NULL DEFAULT N'TrendTag Team',
        [CategoryId] INT NULL,

        -- Status & Publishing
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Published, Archived
        [PublishedAt] DATETIME2 NULL,
        [ViewCount] INT NOT NULL DEFAULT 0,

        -- Timestamps
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,

        -- Foreign Keys
        CONSTRAINT FK_BlogPosts_BlogCategories FOREIGN KEY (CategoryId)
            REFERENCES BlogCategories(Id) ON DELETE SET NULL,

        -- Indexes
        INDEX IX_BlogPosts_Slug (Slug),
        INDEX IX_BlogPosts_Status (Status),
        INDEX IX_BlogPosts_PublishedAt (PublishedAt DESC),
        INDEX IX_BlogPosts_CategoryId (CategoryId),
        INDEX IX_BlogPosts_ViewCount (ViewCount DESC)
    );

    PRINT 'Created BlogPosts table';
END
ELSE
BEGIN
    PRINT 'BlogPosts table already exists';
END
GO

-- =====================================================
-- Table: BlogPostTags (Many-to-Many relationship)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogPostTags')
BEGIN
    CREATE TABLE [dbo].[BlogPostTags] (
        [BlogPostId] INT NOT NULL,
        [BlogTagId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        PRIMARY KEY (BlogPostId, BlogTagId),

        CONSTRAINT FK_BlogPostTags_BlogPosts FOREIGN KEY (BlogPostId)
            REFERENCES BlogPosts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BlogPostTags_BlogTags FOREIGN KEY (BlogTagId)
            REFERENCES BlogTags(Id) ON DELETE CASCADE,

        INDEX IX_BlogPostTags_BlogPostId (BlogPostId),
        INDEX IX_BlogPostTags_BlogTagId (BlogTagId)
    );

    PRINT 'Created BlogPostTags table';
END
ELSE
BEGIN
    PRINT 'BlogPostTags table already exists';
END
GO

-- =====================================================
-- Insert Initial Blog Categories
-- =====================================================
IF NOT EXISTS (SELECT * FROM BlogCategories)
BEGIN
    INSERT INTO [dbo].[BlogCategories] (Name, DisplayNameVi, Slug, Description, IsActive)
    VALUES
        ('TikTok Tips', N'Mẹo TikTok', 'tiktok-tips', N'Các mẹo và chiến lược để tăng view, follower trên TikTok', 1),
        ('Hashtag Strategy', N'Chiến Lược Hashtag', 'hashtag-strategy', N'Hướng dẫn chọn và sử dụng hashtag hiệu quả', 1),
        ('Trending Analysis', N'Phân Tích Trending', 'trending-analysis', N'Phân tích các xu hướng viral trên TikTok', 1),
        ('Creator Guide', N'Hướng Dẫn Creator', 'creator-guide', N'Hướng dẫn toàn diện cho TikTok creator', 1),
        ('Case Studies', N'Case Study', 'case-studies', N'Các case study thành công về tăng trưởng TikTok', 1),
        ('News & Updates', N'Tin Tức & Cập Nhật', 'news-updates', N'Tin tức mới nhất về TikTok và social media', 1);

    PRINT 'Inserted initial blog categories';
END
ELSE
BEGIN
    PRINT 'Blog categories already exist';
END
GO

-- =====================================================
-- Insert Initial Blog Tags
-- =====================================================
IF NOT EXISTS (SELECT * FROM BlogTags)
BEGIN
    INSERT INTO [dbo].[BlogTags] (Name, Slug)
    VALUES
        ('Hashtag Trending', 'hashtag-trending'),
        ('TikTok SEO', 'tiktok-seo'),
        ('Viral Video', 'viral-video'),
        ('FYP Tips', 'fyp-tips'),
        ('TikTok Algorithm', 'tiktok-algorithm'),
        ('Content Strategy', 'content-strategy'),
        ('TikTok Analytics', 'tiktok-analytics'),
        ('Creator Tips', 'creator-tips'),
        ('TikTok Growth', 'tiktok-growth'),
        ('Hashtag Research', 'hashtag-research'),
        ('Video Optimization', 'video-optimization'),
        ('Engagement Tips', 'engagement-tips'),
        ('TikTok Trends 2025', 'tiktok-trends-2025'),
        ('Beginner Guide', 'beginner-guide'),
        ('Advanced Tips', 'advanced-tips');

    PRINT 'Inserted initial blog tags';
END
ELSE
BEGIN
    PRINT 'Blog tags already exist';
END
GO

-- =====================================================
-- Verify Tables Created
-- =====================================================
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name IN ('BlogCategories', 'BlogTags', 'BlogPosts', 'BlogPostTags')
ORDER BY t.name, c.column_id;
GO

-- =====================================================
-- Display Created Tables Count
-- =====================================================
SELECT
    'BlogCategories' AS TableName,
    COUNT(*) AS RecordCount
FROM BlogCategories
UNION ALL
SELECT
    'BlogTags' AS TableName,
    COUNT(*) AS RecordCount
FROM BlogTags
UNION ALL
SELECT
    'BlogPosts' AS TableName,
    COUNT(*) AS RecordCount
FROM BlogPosts
UNION ALL
SELECT
    'BlogPostTags' AS TableName,
    COUNT(*) AS RecordCount
FROM BlogPostTags;
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Blog System Tables Created Successfully!';
PRINT '=====================================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Create C# models for BlogPost, BlogCategory, BlogTag';
PRINT '2. Create IBlogRepository interface';
PRINT '3. Create BlogRepository implementation';
PRINT '4. Create BlogController';
PRINT '5. Create blog views (Index, Details)';
PRINT '6. Write first 3 blog posts';
PRINT '';
GO
